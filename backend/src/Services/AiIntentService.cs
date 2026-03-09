using System.Text.Json;

namespace WebApp;

public static class AiIntentService
{
    public static async Task<AiIntentResult> ExtractIntentAsync(List<AiChatMessage> messages)
    {
        var recentConversation = BuildRecentConversation(messages);

        var systemPrompt = """
You are an intent extraction engine for a cinema assistant.
Your job is to analyze the user request and return ONLY valid JSON.

Allowed intents:
- general.capabilities
- showings.search
- pricing.ticket
- snacks.menu
- snacks.price
- booking.help
- salongs.info
- hours.info
- cinema.info
- unknown

Rules:
- Return JSON only. No markdown. No explanation.
- If the user asks a follow-up question, infer missing filters from the recent conversation when reasonably clear.
- Prefer showings.search for film/showing/date questions.
- For title-only prompts like "Dune?" or "Inception?" prefer showings.search with film_title set.
- For "överimorgon", put that raw phrase into specific_date if you cannot resolve it.
- For "i helgen", put that raw phrase into specific_date if you cannot resolve it.
- For phrases like "måndag nästa vecka", put that raw phrase into specific_date if you cannot resolve it.

date_mode values:
- today
- tomorrow
- tonight
- specific_date
- upcoming
- range
- none

time_of_day values:
- morning
- afternoon
- evening
- night
- none

Output shape:
{
  "intent": "showings.search",
  "confidence": 0.93,
  "needs_clarification": false,
  "clarification_question": "",
  "filters": {
    "date_mode": "tomorrow",
    "specific_date": "",
    "range_start": "",
    "range_end": "",
    "time_of_day": "evening",
    "film_title": "",
    "salong_name": "",
    "genre": "",
    "snack_item": "",
    "ticket_type": "",
    "child_friendly": null,
    "age_rating_min": null,
    "age_rating_max": null
  }
}
""";
        var userPrompt = $"""
Analyze this conversation and extract the latest user intent.

Conversation:
{recentConversation}
""";

        var promptMessages = Arr(
            Obj(new { role = "system", content = systemPrompt }),
            Obj(new { role = "user", content = userPrompt })
        );

        var raw = await AiProxyService.GetAssistantTextAsync(promptMessages);
        var result = ParseIntentJson(raw);
return PostProcessIntent(result, messages);
    }

    private static string BuildRecentConversation(List<AiChatMessage> messages)
    {
        var relevant = messages.TakeLast(8).ToList();
        var lines = new List<string>();

        foreach (var m in relevant)
        {
            lines.Add($"{m.role}: {m.content}");
        }

        return string.Join("\n", lines);
    }

    private static AiIntentResult ParseIntentJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new AiIntentResult();

        raw = raw.Trim();

        // Try to extract JSON object if model wrapped it
        int first = raw.IndexOf('{');
        int last = raw.LastIndexOf('}');
        if (first >= 0 && last > first)
            raw = raw.Substring(first, last - first + 1);

        try
        {
            var result = JsonSerializer.Deserialize<AiIntentResult>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new AiIntentResult();
        }
        catch
        {
            return new AiIntentResult();
        }
    }

    private static AiIntentResult PostProcessIntent(AiIntentResult result, List<AiChatMessage> messages)
{
    if (result == null) result = new AiIntentResult();
    if (result.filters == null) result.filters = new AiIntentFilters();

    var latestUser = messages.LastOrDefault(x => x.role == "user")?.content ?? "";
    var latestLower = latestUser.ToLowerInvariant().Trim();

    // handle title-only prompts like "dune?" or "inception?"
    bool looksLikeBareTitle =
        !string.IsNullOrWhiteSpace(latestUser) &&
        latestUser.Length <= 40 &&
        !latestLower.Contains("pris") &&
        !latestLower.Contains("öppet") &&
        !latestLower.Contains("salong") &&
        !latestLower.Contains("boka") &&
        !latestLower.Contains("kiosk");

    if ((result.intent == "unknown" || result.intent == "showings.search") &&
        string.IsNullOrWhiteSpace(result.filters.film_title) &&
        looksLikeBareTitle)
    {
        var candidate = AiTitleNormalizationService.ExtractLikelyTitleFromPrompt(latestUser);
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            result.intent = "showings.search";
            result.filters.film_title = candidate;

            if (string.IsNullOrWhiteSpace(result.filters.date_mode))
                result.filters.date_mode = "upcoming";
        }
    }

        if (latestLower.Contains("väder") ||
            latestLower.Contains("champions league") ||
            latestLower.Contains("fotboll") ||
            latestLower.Contains("aktier") ||
            latestLower.Contains("bitcoin"))
        {
            result.intent = "unknown";
            result.filters = new AiIntentFilters();
            return result;
        }

    // normalize explicit title filter if present
    if (!string.IsNullOrWhiteSpace(result.filters.film_title))
        result.filters.film_title = AiTitleNormalizationService.NormalizeTitleCandidate(result.filters.film_title);

    // soft date normalization from raw latest prompt
    if (string.IsNullOrWhiteSpace(result.filters.specific_date))
    {
        if (latestLower.Contains("överimorgon"))
            result.filters.specific_date = "överimorgon";
        else if (latestLower.Contains("i helgen"))
            result.filters.specific_date = "i helgen";
        else if (latestLower.Contains("nästa vecka"))
        {
            if (latestLower.Contains("måndag")) result.filters.specific_date = "måndag nästa vecka";
            else if (latestLower.Contains("tisdag")) result.filters.specific_date = "tisdag nästa vecka";
            else if (latestLower.Contains("onsdag")) result.filters.specific_date = "onsdag nästa vecka";
            else if (latestLower.Contains("torsdag")) result.filters.specific_date = "torsdag nästa vecka";
            else if (latestLower.Contains("fredag")) result.filters.specific_date = "fredag nästa vecka";
            else if (latestLower.Contains("lördag")) result.filters.specific_date = "lördag nästa vecka";
            else if (latestLower.Contains("söndag")) result.filters.specific_date = "söndag nästa vecka";
        }
    }

    return result;
}
}