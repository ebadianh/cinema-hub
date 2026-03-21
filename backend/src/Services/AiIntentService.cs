using System.Text.Json;
using System.Text.RegularExpressions;

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
            lines.Add($"{m.role}: {m.content}");

        return string.Join("\n", lines);
    }

    private static AiIntentResult ParseIntentJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new AiIntentResult();

        raw = raw.Trim();

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
        var tinyPrompt = latestLower.Trim();

        if (IsGreetingPrompt(tinyPrompt))
        {
            result.intent = "general.capabilities";
            result.needs_clarification = false;
            result.clarification_question = "";
            result.filters = new AiIntentFilters();
            return result;
        }

        if (IsAffirmativeFollowUp(tinyPrompt))
        {
            result.intent = "showings.search";
            result.needs_clarification = false;
            result.clarification_question = "";

            // Keep this broad to avoid carrying stale title filters from prior turns.
            result.filters.film_title = "";
            result.filters.salong_name = "";
            result.filters.genre = "";

            if (string.IsNullOrWhiteSpace(result.filters.date_mode))
                result.filters.date_mode = "upcoming";

            return result;
        }

        if (tinyPrompt == "när" || tinyPrompt == "var" || tinyPrompt == "hur")
        {
            result.needs_clarification = true;
            result.clarification_question = "Menar du en specifik film eller visning?";
            return result;
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

        if (LooksLikeFullInfoRequest(latestLower))
        {
            result.intent = "general.capabilities";
            result.needs_clarification = false;
            result.clarification_question = "";
            result.filters = new AiIntentFilters();
            return result;
        }

        if (LooksLikeGenericSnackPricePrompt(latestLower))
        {
            result.intent = "snacks.menu";
            result.needs_clarification = false;
            result.clarification_question = "";
            result.filters.snack_item = "";
            return result;
        }

    bool looksLikeBroadShowingsPrompt =
     latestLower.Contains("vilka filmer") ||
     latestLower.Contains("film visas") ||
     latestLower.Contains("filmer visas") ||
     latestLower.Contains("vad går") ||
     latestLower.Contains("vad visas") ||
     latestLower.Contains("visa visningar") ||
     latestLower.Contains("vilka visningar") ||
     latestLower.Contains("visningar") ||
     latestLower.Contains("salongen") ||
     latestLower.Contains("salong") ||
     latestLower.Contains("idag") ||
     latestLower.Contains("imorgon") ||
     latestLower.Contains("ikväll") ||
     latestLower.Contains("i helgen");

    bool looksLikeBareTitle =
    !string.IsNullOrWhiteSpace(latestUser) &&
    latestUser.Length <= 40 &&
    !looksLikeBroadShowingsPrompt &&
    !latestLower.Contains("pris") &&
    !latestLower.Contains("öppet") &&
    !latestLower.Contains("salong") &&
    !latestLower.Contains("boka") &&
    !latestLower.Contains("kiosk") &&
    !latestLower.Contains("hjälp") &&
    !latestLower.Contains("vad kan du") &&
    !latestLower.Contains("öppettider");

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

        if (!string.IsNullOrWhiteSpace(result.filters.film_title))
            result.filters.film_title = AiTitleNormalizationService.NormalizeTitleCandidate(result.filters.film_title);

        var filmTitleLower = (result.filters.film_title ?? "").Trim().ToLowerInvariant();

        bool invalidBroadFilmTitle =
        filmTitleLower.Contains("vilka filmer") ||
        filmTitleLower.Contains("vad går") ||
        filmTitleLower.Contains("vad visas") ||
        filmTitleLower.Contains("visningar") ||
        filmTitleLower.Contains("salong") ||
        filmTitleLower.Contains("idag") ||
        filmTitleLower.Contains("imorgon") ||
        filmTitleLower.Contains("ikväll");

        if (invalidBroadFilmTitle)
        {
            result.filters.film_title = "";
        }

        if (LooksLikeBroadShowingsRequest(latestLower))
        {
            result.filters.film_title = "";
        }
        
        // Stronger raw date phrase extraction from latest prompt
        var detectedRawDatePhrase = "";

        if (latestLower.Contains("överimorgon"))
            detectedRawDatePhrase = "överimorgon";
        else if (latestLower.Contains("i helgen"))
            detectedRawDatePhrase = "i helgen";
        else if (latestLower.Contains("nästa vecka"))
        {
            if (latestLower.Contains("måndag")) detectedRawDatePhrase = "måndag nästa vecka";
            else if (latestLower.Contains("tisdag")) detectedRawDatePhrase = "tisdag nästa vecka";
            else if (latestLower.Contains("onsdag")) detectedRawDatePhrase = "onsdag nästa vecka";
            else if (latestLower.Contains("torsdag")) detectedRawDatePhrase = "torsdag nästa vecka";
            else if (latestLower.Contains("fredag")) detectedRawDatePhrase = "fredag nästa vecka";
            else if (latestLower.Contains("lördag")) detectedRawDatePhrase = "lördag nästa vecka";
            else if (latestLower.Contains("söndag")) detectedRawDatePhrase = "söndag nästa vecka";
        }
        else
        {
            if (latestLower.Contains("måndag")) detectedRawDatePhrase = "måndag";
            else if (latestLower.Contains("tisdag")) detectedRawDatePhrase = "tisdag";
            else if (latestLower.Contains("onsdag")) detectedRawDatePhrase = "onsdag";
            else if (latestLower.Contains("torsdag")) detectedRawDatePhrase = "torsdag";
            else if (latestLower.Contains("fredag")) detectedRawDatePhrase = "fredag";
            else if (latestLower.Contains("lördag")) detectedRawDatePhrase = "lördag";
            else if (latestLower.Contains("söndag")) detectedRawDatePhrase = "söndag";
        }

        if (!string.IsNullOrWhiteSpace(detectedRawDatePhrase))
        {
            if (result.intent == "unknown")
                result.intent = "showings.search";

            result.filters.date_mode = "specific_date";
            result.filters.specific_date = detectedRawDatePhrase;
        }

        var requestedAge = TryExtractRequestedAge(latestLower);
        var childFriendlyQuery = LooksLikeChildFriendlyQuery(latestLower);
        var explicitDateScope = HasExplicitDateScope(latestLower);
        var explicitTimeOfDayScope = HasExplicitTimeOfDayScope(latestLower);
        var ageFilteredShowingsQuery = requestedAge != null || childFriendlyQuery;

        if (requestedAge != null)
        {
            result.intent = "showings.search";
            result.filters.age_rating_max = requestedAge.Value;
            result.filters.child_friendly = requestedAge.Value <= 11;
        }

        if (childFriendlyQuery)
        {
            result.intent = "showings.search";
            result.filters.child_friendly = true;
            if (result.filters.age_rating_max == null)
                result.filters.age_rating_max = 11;
        }

        if (ageFilteredShowingsQuery)
        {
            result.intent = "showings.search";

            if (!explicitDateScope)
            {
                result.filters.date_mode = "upcoming";
                result.filters.specific_date = "";
                result.filters.range_start = "";
                result.filters.range_end = "";
            }

            if (!explicitTimeOfDayScope)
                result.filters.time_of_day = "";
        }

        // Fallback: obvious showings prompts should still search showings
        bool looksLikeShowingsPrompt =
            latestLower.Contains("film") ||
            latestLower.Contains("filmer") ||
            latestLower.Contains("visning") ||
            latestLower.Contains("visningar") ||
            latestLower.Contains("går") ||
            latestLower.Contains("visas") ||
            latestLower.Contains("showing") ||
            latestLower.Contains("imorgon") ||
            latestLower.Contains("idag") ||
            latestLower.Contains("ikväll");

        if (result.intent == "unknown" && looksLikeShowingsPrompt)
        {
            result.intent = "showings.search";
            if (string.IsNullOrWhiteSpace(result.filters.date_mode))
                result.filters.date_mode = "upcoming";
        }

        if (result.intent == "unknown")
            ApplyDomainFallbackIntent(result, latestLower);

        return result;
    }

    private static void ApplyDomainFallbackIntent(AiIntentResult result, string prompt)
    {
        if (LooksLikeGenericSnackPricePrompt(prompt))
        {
            result.intent = "snacks.menu";
            result.filters.snack_item = "";
            return;
        }

        if (LooksLikeSnackPrompt(prompt))
        {
            var extractedSnackItem = ExtractSnackItemFromPrompt(prompt);
            var asksForPrice = prompt.Contains("pris") || prompt.Contains("kostar");

            result.intent = asksForPrice && !string.IsNullOrWhiteSpace(extractedSnackItem)
                ? "snacks.price"
                : "snacks.menu";

            result.filters.snack_item = asksForPrice ? extractedSnackItem : "";
            return;
        }

        if (LooksLikeTicketPricePrompt(prompt))
        {
            result.intent = "pricing.ticket";
            return;
        }

        if (LooksLikeHoursPrompt(prompt))
        {
            result.intent = "hours.info";
            return;
        }

        if (LooksLikeBookingHelpPrompt(prompt))
        {
            result.intent = "booking.help";
            return;
        }

        if (LooksLikeSalongPrompt(prompt))
        {
            result.intent = "salongs.info";
            return;
        }

        if (LooksLikeCinemaConceptPrompt(prompt))
        {
            result.intent = "cinema.info";
            return;
        }
    }

    private static bool IsGreetingPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var normalizedPrompt = Regex.Replace(prompt, "[^\\p{L}\\p{N}\\s]", " ").Trim();
        normalizedPrompt = Regex.Replace(normalizedPrompt, "\\s+", " ");

        var greetingSet = new HashSet<string>
        {
            "hej",
            "hejsan",
            "hej hej",
            "tjena",
            "tjenare",
            "hallå",
            "halloj",
            "god morgon",
            "god kväll",
            "hello",
            "hi"
        };

        return greetingSet.Contains(normalizedPrompt);
    }

    private static bool IsAffirmativeFollowUp(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var normalizedPrompt = Regex.Replace(prompt, "[^\\p{L}\\p{N}\\s]", " ").Trim();
        normalizedPrompt = Regex.Replace(normalizedPrompt, "\\s+", " ");

        return normalizedPrompt == "ja" ||
               normalizedPrompt == "japp" ||
               normalizedPrompt == "yes" ||
               normalizedPrompt == "absolut" ||
               normalizedPrompt == "gärna";
    }

    private static bool LooksLikeBroadShowingsRequest(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        bool asksForPluralMoviesOrShowings =
            prompt.Contains("filmer") ||
            prompt.Contains("visningar") ||
            prompt.Contains("vilka filmer") ||
            prompt.Contains("andra filmer") ||
            prompt.Contains("alla filmer");

        bool asksForDateScope =
            prompt.Contains("idag") ||
            prompt.Contains("imorgon") ||
            prompt.Contains("ikväll") ||
            prompt.Contains("i helgen") ||
            prompt.Contains("nästa vecka") ||
            prompt.Contains("måndag") ||
            prompt.Contains("tisdag") ||
            prompt.Contains("onsdag") ||
            prompt.Contains("torsdag") ||
            prompt.Contains("fredag") ||
            prompt.Contains("lördag") ||
            prompt.Contains("söndag");

        bool isGenericInventoryQuestion =
            prompt.Contains("vilka filmer har ni") ||
            prompt.Contains("har ni andra filmer") ||
            prompt.Contains("filmer ni har");

        return (asksForPluralMoviesOrShowings && asksForDateScope) || isGenericInventoryQuestion;
    }

    private static bool LooksLikeFullInfoRequest(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var explicitAllInfoRequest =
            ContainsAny(prompt,
                "all denna info",
                "all den här infon",
                "all den har infon",
                "all info",
                "sammanställ",
                "sammanfatta",
                "ge mig all",
                "hela infon") ||
            prompt.Contains("som användare vill jag kunna prata med en ai-assistent");

        var signalCount = 0;
        if (LooksLikeHoursPrompt(prompt)) signalCount++;
        if (LooksLikeTicketPricePrompt(prompt)) signalCount++;
        if (LooksLikeSnackPrompt(prompt)) signalCount++;
        if (LooksLikeBookingHelpPrompt(prompt)) signalCount++;
        if (LooksLikeSalongPrompt(prompt)) signalCount++;
        if (LooksLikeShowingsPrompt(prompt)) signalCount++;
        if (LooksLikeCinemaConceptPrompt(prompt)) signalCount++;

        return explicitAllInfoRequest || signalCount >= 4;
    }

    private static bool LooksLikeShowingsPrompt(string prompt)
    {
        return ContainsAny(prompt,
            "film",
            "filmer",
            "visning",
            "visningar",
            "vad går",
            "vad visas",
            "imorgon",
            "idag",
            "ikväll",
            "överimorgon",
            "i helgen");
    }

    private static bool LooksLikeTicketPricePrompt(string prompt)
    {
        return ContainsAny(prompt,
            "biljettpris",
            "biljettpriser",
            "vad kostar era biljetter",
            "vad kostar biljetter",
            "pris på biljetter",
            "biljetter kostar");
    }

    private static bool LooksLikeHoursPrompt(string prompt)
    {
        return ContainsAny(prompt,
            "öppettider",
            "öppettid",
            "när öppnar",
            "när stänger",
            "hur länge har ni öppet",
            "öppet");
    }

    private static bool LooksLikeSnackPrompt(string prompt)
    {
        return ContainsAny(prompt,
            "snack",
            "kiosk",
            "popcorn",
            "godis",
            "dryck",
            "läsk",
            "nachos",
            "gourmetpopcorn",
            "energidryck",
            "juice",
            "kaffe",
            "te");
    }

    private static bool LooksLikeGenericSnackPricePrompt(string prompt)
    {
        if (!LooksLikeSnackPrompt(prompt))
            return false;

        if (!prompt.Contains("pris") && !prompt.Contains("kostar"))
            return false;

        return string.IsNullOrWhiteSpace(ExtractSnackItemFromPrompt(prompt));
    }

    private static bool LooksLikeBookingHelpPrompt(string prompt)
    {
        return ContainsAny(prompt,
            "hur bokar",
            "boka",
            "bokning",
            "köpa biljett",
            "köper biljett",
            "hjälp med bokning");
    }

    private static bool LooksLikeSalongPrompt(string prompt)
    {
        return ContainsAny(prompt,
            "salong",
            "salonger",
            "hur stor",
            "storlek",
            "antal platser",
            "platser i salongen");
    }

    private static bool LooksLikeCinemaConceptPrompt(string prompt)
    {
        return ContainsAny(prompt,
            "vad står er bio för",
            "vad står ni för",
            "inriktning",
            "koncept",
            "om biografen",
            "vad är cinemamob");
    }

    private static string ExtractSnackItemFromPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return "";

        var knownSnackTerms = new[]
        {
            "smörpopcorn",
            "salt popcorn",
            "gourmetpopcorn",
            "popcorn",
            "nachos",
            "godis",
            "choklad",
            "chokladpraliner",
            "läsk",
            "mineralvatten",
            "juice",
            "kaffe",
            "te",
            "energidryck",
            "glass",
            "desserter",
            "säsongsbaserade snacks"
        };

        foreach (var term in knownSnackTerms)
        {
            if (prompt.Contains(term))
                return term;
        }

        return "";
    }

    private static bool ContainsAny(string prompt, params string[] terms)
    {
        foreach (var term in terms)
        {
            if (prompt.Contains(term))
                return true;
        }

        return false;
    }

    private static int? TryExtractRequestedAge(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return null;

        var match = Regex.Match(prompt, "\\b(\\d{1,2})\\s*år\\b");
        if (!match.Success)
            return null;

        if (!int.TryParse(match.Groups[1].Value, out var parsedAge))
            return null;

        if (parsedAge < 0 || parsedAge > 20)
            return null;

        return parsedAge;
    }

    private static bool LooksLikeChildFriendlyQuery(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        return prompt.Contains("barnvänlig") ||
               prompt.Contains("barnvänliga") ||
               prompt.Contains("barntillåten") ||
               prompt.Contains("för barn") ||
               prompt.Contains("barnfilm") ||
               prompt.Contains("barnfilmer") ||
               prompt.Contains("familjefilm") ||
               prompt.Contains("familjefilmer") ||
               prompt.Contains("min son") ||
               prompt.Contains("min dotter");
    }

    private static bool HasExplicitDateScope(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        return ContainsAny(prompt,
            "idag",
            "imorgon",
            "imorn",
            "ikväll",
            "överimorgon",
            "i helgen",
            "nästa vecka",
            "måndag",
            "tisdag",
            "onsdag",
            "torsdag",
            "fredag",
            "lördag",
            "söndag");
    }

    private static bool HasExplicitTimeOfDayScope(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        return ContainsAny(prompt,
            "morgon",
            "förmiddag",
            "eftermiddag",
            "kväll",
            "ikväll",
            "natt");
    }
}