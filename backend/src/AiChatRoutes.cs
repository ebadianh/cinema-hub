using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace WebApp;

public static class AiChatRoutes
{
    // -----------------------------
    // vad är detta?
    // -----------------------------
    // denna fil gör en sak:
    // - tar emot frontendens chat-meddelanden (/api/chat)
    // - plockar ut senaste user-frågan
    // - hämtar relevanta "fakta" från db + cinema-facts.json
    // - skickar allt (systemprompt + fakta + chat-historik) vidare till ai-proxyn
    //
    // viktigt: ai:n får bara svara med det som står i "fakta"-delen vi skickar in.

    private static string aiAccessToken = "";
    private static string systemPrompt = "";
    private static dynamic cinemaFacts = null;

    // nodehill-proxy som pratar med ai-modellen
    private static readonly string proxyUrl = "https://ai-api.nodehill.com";
    private static readonly HttpClient httpClient = new HttpClient();

    public static void Start()
    {
        // 1) ladda konfig och statiska filer en gång när servern startar
        LoadConfig();
        LoadSystemPrompt();
        LoadCinemaFacts();

        // 2) main endpoint: frontend postar hit { messages: [...] }
        // messages är chat-historiken (roll + content)
        App.MapPost("/api/chat", async (HttpContext context, JsonElement bodyJson) =>
        {
            try
            {
                // parse json som kommer från frontenden
                var body = JSON.Parse(bodyJson.ToString());
                var messages = (Arr)body.messages;

                if (messages == null)
                    return RestResult.Parse(context, new { error = "messages array is required." });

                // plocka ut senaste användartexten (t.ex. "vilka visningar finns idag?")
                var userText = ExtractLatestUserText(messages);

                // bygg upp en markdown-sträng med fakta, baserat på userText
                // (vi kör inte "magiskt ai" här, vi gör vanliga sql-frågor)
                var cinemaContext = BuildCinemaContextMarkdown(userText, context);

                // 3) skapa meddelande-listan vi skickar till ai:
                // - systemprompt (regler + stil)
                // - "cinema facts (källa)" som ai:n måste hålla sig till
                // - hela chat-historiken från user (så följdfrågor funkar)
                var fullMessages = Arr();

                if (!string.IsNullOrEmpty(systemPrompt))
                    fullMessages.Push(Obj(new { role = "system", content = systemPrompt }));

                if (!string.IsNullOrWhiteSpace(cinemaContext))
                {
                    fullMessages.Push(Obj(new
                    {
                        role = "system",
                        content =
                            "## cinemahub fakta (källa)\n" +
                            "använd endast fakta från denna sektion när du svarar.\n" +
                            "om något saknas: säg att du inte vet och hänvisa användaren till rätt sida (/about eller startsidan).\n\n" +
                            cinemaContext
                    }));
                }

                // chat-historiken från frontenden (user/assistant)
                for (int i = 0; i < messages.Length; i++)
                    fullMessages.Push(messages[i]);

                var requestBody = Obj(new { messages = fullMessages });

                // 4) skicka vidare till ai-proxy
                var request = new HttpRequestMessage(HttpMethod.Post, $"{proxyUrl}/v1/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {aiAccessToken}");
                request.Content = new StringContent(
                    JSON.Stringify(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                // om ai-proxyn svarar med fel: returnera det som json
                if (!response.IsSuccessStatusCode)
                {
                    var error = JSON.Parse(responseContent);
                    return RestResult.Parse(context, error);
                }

                // annars: returnera ai-svaret till frontenden
                var data = JSON.Parse(responseContent);
                return RestResult.Parse(context, data);
            }
            catch (Exception ex)
            {
                // om nåt går sönder: returnera fel
                return RestResult.Parse(context, new { error = ex.Message });
            }
        });

        // 5) debug endpoint (frivillig men bra)
        // använd den för att se om db/viewn fungerar alls:
        // öppna i browser: /api/debug/showings
        App.MapGet("/api/debug/showings", (HttpContext context) =>
        {
            var rows = DbQuery.SQLQuery(@"
                SELECT id, film_title, salong_name, start_time, age_rating, genre
                FROM showings_detail
                ORDER BY start_time
                LIMIT 10
            ", null, context);

            return RestResult.Parse(context, new { count = rows?.Length ?? 0, rows });
        });
    }

    // -----------------------------
    // hjälpmetoder: chat parsing
    // -----------------------------

    // går bakifrån i messages och tar senaste message där role == "user"
    private static string ExtractLatestUserText(Arr messages)
    {
        if (messages == null || messages.Length == 0) return "";

        for (int i = messages.Length - 1; i >= 0; i--)
        {
            try
            {
                var m = messages[i];
                var role = (string)m.role;

                if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
                    return (string)m.content ?? "";
            }
            catch
            {
                // om ett message är konstigt formaterat: skippa bara
            }
        }

        return "";
    }

    // -----------------------------
    // huvudgrejen: bygg "fakta (källa)"
    // -----------------------------
    // tanken:
    // - vi gör enkel intent-detektion på userText
    // - beroende på vad user frågar om hämtar vi bara relevant data
    // - resultatet blir en markdown som ai:n får använda som "enda källa"
private static string BuildCinemaContextMarkdown(string userText, HttpContext context)
{
    var t = (userText ?? "").ToLowerInvariant().Trim();

    // intent: visningar / filmer
    bool wantsShowings =
        t.Contains("vilka filmer") || t.Contains("vad går") || t.Contains("vad visas") ||
        t.Contains("visas") || t.Contains("visning") || t.Contains("visningar") ||
        t.Contains("föreställ") || t.Contains("föreställning") ||
        t.Contains("idag") || t.Contains("imorgon") || LooksLikeDate(t);

    // intent: priser
    bool wantsPrices =
        t.Contains("pris") || t.Contains("kostar") || t.Contains("biljett") ||
        t.Contains("pension") || t.Contains("barn") || t.Contains("vuxen");

    // intent: salonger / antal platser
    bool wantsSalongs =
        t.Contains("salong") || t.Contains("platser") || t.Contains("stora") || t.Contains("lilla") || t.Contains("storlek") ||
        t.Contains("sammanlagt") || t.Contains("totalt");

    // intent: öppettider
    bool wantsHours =
        t.Contains("öppet") || t.Contains("öppettid") || t.Contains("öppettider");

    // intent: snacks
    bool wantsSnacks =
        t.Contains("snack") || t.Contains("kiosk") || t.Contains("popcorn") || t.Contains("godis") || t.Contains("utbud");

    // intent: hur bokar man
    bool wantsBooking =
        t.Contains("boka") || t.Contains("bokar") || t.Contains("bokning") || (t.Contains("hur") && t.Contains("biljett"));

    // intent: åldersgränser
    bool wantsAge = WantsAgeRating(t);

    // bara för riktigt generella "vad kan du göra?"-frågor
    bool wantsCapabilities =
        t.Contains("vad kan du göra") ||
        t.Contains("hjälpa med") ||
        t.Contains("vad hjälper du med") ||
        t == "hej" || t == "hejsan" || t == "tjena";

    var sb = new StringBuilder();

    // -----------------------------
    // fasta fakta från cinema-facts.json
    // -----------------------------

    if (cinemaFacts != null && (wantsHours || wantsCapabilities))
    {
        try
        {
            var oh = cinemaFacts.openingHours;
            if (oh != null)
            {
                sb.AppendLine("### öppettider");
                sb.AppendLine($"- mån–tors: {oh.monThu}");
                sb.AppendLine($"- fre: {oh.fri}");
                sb.AppendLine($"- lör: {oh.sat}");
                sb.AppendLine($"- sön: {oh.sun}");
                sb.AppendLine();
            }
        }
        catch { }
    }

    if (cinemaFacts != null && (t.Contains("inrikt") || t.Contains("vision") || t.Contains("om biografen") || wantsCapabilities))
    {
        try
        {
            var concept = (string)cinemaFacts.concept;
            if (!string.IsNullOrWhiteSpace(concept))
            {
                sb.AppendLine("### inriktning");
                sb.AppendLine(concept.Trim());
                sb.AppendLine();
            }
        }
        catch { }
    }

    if (cinemaFacts != null && wantsSnacks)
    {
        try
        {
            var snacks = cinemaFacts.snacks;
            if (snacks != null)
            {
                sb.AppendLine("### kiosk / snacks-utbud");
                AppendStringArray(sb, "klassiker", snacks.classics);
                AppendStringArray(sb, "dryck", snacks.drinks);
                AppendStringArray(sb, "premium", snacks.premium);
                sb.AppendLine();
            }
        }
        catch { }
    }

    // -----------------------------
    // db-fakta: biljettpriser
    // -----------------------------
    if (wantsPrices)
    {
        var prices = DbQuery.SQLQuery(
            "SELECT name, price FROM Ticket_Type ORDER BY price DESC",
            null,
            context
        );

        if (prices != null && prices.Length > 0)
        {
            sb.AppendLine("### biljettpriser");
            for (int i = 0; i < prices.Length; i++)
            {
                var p = prices[i];
                sb.AppendLine($"- {(string)p.name}: {(p.price)} kr");
            }
            sb.AppendLine();
        }
    }

    // -----------------------------
    // db-fakta: salonger + antal platser
    // -----------------------------
    if (wantsSalongs)
    {
        var salongsSql = @"
            SELECT sa.id, sa.name, COUNT(se.id) AS seats
            FROM Salongs sa
            LEFT JOIN Seats se ON se.salong_id = sa.id
            GROUP BY sa.id, sa.name
            ORDER BY sa.id
        ".Trim();

        var salongs = DbQuery.SQLQuery(salongsSql, null, context);

        sb.AppendLine("### salonger (antal platser)");

        if (salongs != null && salongs.Length > 0)
        {
            int total = 0;

            for (int i = 0; i < salongs.Length; i++)
            {
                var s = salongs[i];
                int seats = 0;

                int.TryParse(s.seats?.ToString() ?? "0", out seats);
                total += seats;

                sb.AppendLine($"- {(string)s.name}: {seats} platser");
            }

            if (t.Contains("sammanlagt") || t.Contains("totalt"))
                sb.AppendLine($"- totalt: {total} platser");

            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("- inga salonger hittades i databasen.");
            sb.AppendLine();
        }
    }

    // -----------------------------
    // db-fakta: visningar
    // -----------------------------
    if (wantsShowings || wantsAge)
    {
        DateTime? from = null;
        DateTime? to = null;

        if (t.Contains("imorgon"))
        {
            var d = DateTime.Today.AddDays(1);
            from = d;
            to = d.AddDays(1);
        }
        else if (t.Contains("idag"))
        {
            var d = DateTime.Today;
            from = d;
            to = d.AddDays(1);
        }
        else
        {
            var parsed = TryParseDateFromText(t);
            if (parsed != null)
            {
                from = parsed.Value.Date;
                to = parsed.Value.Date.AddDays(1);
            }
        }

        string salongLike = null;
        if (t.Contains("stora")) salongLike = "%Stora%";
        else if (t.Contains("lilla")) salongLike = "%Lilla%";

        string filmLike = TryExtractFilmQuery(t);

        var where = new StringBuilder("WHERE 1=1 ");
        var paramObj = Obj();

        if (from != null && to != null)
        {
            where.Append("AND start_time >= @from AND start_time < @to ");
            paramObj.from = from.Value.ToString("yyyy-MM-dd HH:mm:ss");
            paramObj.to = to.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        if (!string.IsNullOrWhiteSpace(salongLike))
        {
            where.Append("AND salong_name LIKE @salong ");
            paramObj.salong = salongLike;
        }

        if (!string.IsNullOrWhiteSpace(filmLike))
        {
            where.Append("AND film_title LIKE @film ");
            paramObj.film = $"%{filmLike}%";
        }

        try
        {
            var showingsSql = $@"
                SELECT
                    id,
                    film_title,
                    salong_name,
                    start_time,
                    language,
                    subtitle,
                    age_rating,
                    genre
                FROM showings_detail
                {where}
                ORDER BY start_time
                LIMIT 25
            ".Trim();

            var showings = DbQuery.SQLQuery(showingsSql, paramObj, context);

            sb.AppendLine("### aktuella visningar");

            if (showings != null && showings.Length > 0)
            {
                for (int i = 0; i < showings.Length; i++)
                {
                    var sh = showings[i];
                    var ar = sh.age_rating == null ? "okänd" : sh.age_rating.ToString();
                    var start = sh.start_time?.ToString() ?? "";
                    var sub = sh.subtitle == null ? "ingen" : sh.subtitle.ToString();

                    sb.AppendLine(
                        $"- #{(sh.id)} — {(string)sh.film_title} — {(string)sh.salong_name} — {start} — språk: {(string)sh.language} — text: {sub} — åldersgräns: {ar}"
                    );
                }

                sb.AppendLine();
                sb.AppendLine("bokningslänk: använd `/booking/<visnings-id>` (t.ex. `/booking/123`).");
                sb.AppendLine();

                AppendAgeFilteredHints(sb, showings, t);
            }
            else
            {
                sb.AppendLine("- inga visningar hittades med de filtren.");
                sb.AppendLine();
            }
        }
        catch
        {
            sb.AppendLine("### aktuella visningar");
            sb.AppendLine("- kunde inte hämta visningar just nu.");
            sb.AppendLine();
        }
    }

    // -----------------------------
    // bokningsflöde
    // -----------------------------
    if (wantsBooking)
    {
        sb.AppendLine("### bokningsflöde (i appen)");
        sb.AppendLine("1) gå till startsidan och välj en visning");
        sb.AppendLine("2) öppna bokningssidan: /booking/<visnings-id>");
        sb.AppendLine("3) välj platser");
        sb.AppendLine("4) välj biljettyp och fyll i email");
        sb.AppendLine("5) bekräfta i flödet i appen");
        sb.AppendLine();
    }

    return sb.ToString().Trim();
}
    // -----------------------------
    // små helpers för snacks-listor
    // -----------------------------
 private static void AppendStringArray(StringBuilder sb, string title, dynamic arr)
{
    try
    {
        if (arr == null) return;

        sb.AppendLine($"- {title}:");

        foreach (var item in arr)
        {
            try
            {
                if (item.name != null)
                {
                    var priceText = item.price != null ? $" ({item.price} kr)" : "";
                    sb.AppendLine($"  - {item.name}{priceText}");
                }
                else
                {
                    sb.AppendLine($"  - {item}");
                }
            }
            catch
            {
                sb.AppendLine($"  - {item}");
            }
        }
    }
    catch
    {
    }
}

    // -----------------------------
    // datum/film parsing (enkelt med regex)
    // -----------------------------
    private static bool LooksLikeDate(string t)
    {
        // ex: 2026-03-03 eller 2026/03/03
        return System.Text.RegularExpressions.Regex.IsMatch(t, @"\b20\d{2}[-/]\d{2}[-/]\d{2}\b");
    }

    private static DateTime? TryParseDateFromText(string t)
    {
        var m = System.Text.RegularExpressions.Regex.Match(t, @"\b(20\d{2})[-/](\d{2})[-/](\d{2})\b");
        if (!m.Success) return null;

        if (int.TryParse(m.Groups[1].Value, out var y) &&
            int.TryParse(m.Groups[2].Value, out var mo) &&
            int.TryParse(m.Groups[3].Value, out var d))
        {
            try { return new DateTime(y, mo, d); } catch { return null; }
        }
        return null;
    }

private static string TryExtractFilmQuery(string t)
{
    if (string.IsNullOrWhiteSpace(t)) return null;

    t = t.ToLowerInvariant().Trim();

    // Breda frågor = ingen titelgissning
    var broadPhrases = new[]
    {
        "vilka filmer",
        "vad går",
        "vad visas",
        "vilka visningar",
        "filmer idag",
        "filmer imorgon",
        "visningar idag",
        "visningar imorgon",
        "finns det för filmer",
        "finns det några filmer"
    };

    for (int i = 0; i < broadPhrases.Length; i++)
    {
        if (t.Contains(broadPhrases[i]))
            return null;
    }

    var stop = new HashSet<string>(new[]
    {
        "visa","visas","visning","visningar","föreställning","föreställningar",
        "idag","imorgon","salong","stora","lilla","pris","priser","biljett","biljetter",
        "bokning","boka","öppettider","öppet","kiosk","snacks","film","filmer","när",
        "vilka","vad","som","det","den","de","finns","går","på","bio","är","kan",
        "åldersgräns","barnvänliga","barnvänlig","över","från","plus","varför","hej","tjena",
        "mig","du","hjälpa","med"
    });

    var parts = (t ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    for (int i = parts.Length - 1; i >= 0; i--)
    {
        var w = parts[i].Trim().Trim(',', '.', '!', '?', ':', ';', '"', '\'');
        if (w.Length >= 3 && !stop.Contains(w))
            return w;
    }

    return null;
}
    // -----------------------------
    // åldersgräns-logik (endast urval, inget hittas på)
    // -----------------------------
    private static bool WantsAgeRating(string t)
    {
        if (string.IsNullOrWhiteSpace(t)) return false;
        t = t.ToLowerInvariant();

        return t.Contains("ålders") ||
               t.Contains("barnvän") ||
               t.Contains("barnvänlig") ||
               t.Contains("för barn") ||
               t.Contains("15+") ||
               t.Contains("över 15") ||
               t.Contains("från 15") ||
               t.Contains("18+") ||
               t.Contains("över 18") ||
               t.Contains("från 18");
    }
    private static int? TryParseAge(dynamic ageRating)
    {
        if (ageRating == null) return null;
    
        string s = ageRating.ToString();
        int n; // <-- explicit typ, inga "out var"
    
        if (int.TryParse(s, out n))
            return n;
    
        return null;
    }
    private static void AppendAgeFilteredHints(StringBuilder sb, Arr showings, string t)
    {
        // vi definierar inga "barnvänliga" på känsla.
        // vi gör bara urval baserat på age_rating i db.
        // (justera gränserna om du vill)

        var wantsKidFriendly = t.Contains("barnvän") || t.Contains("för barn");
        var wantsOver15 = t.Contains("över 15") || t.Contains("från 15") || t.Contains("15+");

        if (!wantsKidFriendly && !wantsOver15) return;

        if (wantsKidFriendly)
        {
            sb.AppendLine("### urval: barnvänliga (baserat på åldersgräns <= 7)");
            bool any = false;

            for (int i = 0; i < showings.Length; i++)
            {
                var sh = showings[i];
                var ar = TryParseAge(sh.age_rating);
                if (ar != null && ar.Value <= 7)
                {
                    any = true;
                    sb.AppendLine($"- #{(sh.id)} — {(string)sh.film_title} — {sh.start_time} — åldersgräns: {ar}");
                }
            }

            if (!any) sb.AppendLine("- inga visningar matchade (age_rating <= 7).");
            sb.AppendLine();
        }

        if (wantsOver15)
        {
            sb.AppendLine("### urval: 15+ (baserat på åldersgräns >= 15)");
            bool any = false;

            for (int i = 0; i < showings.Length; i++)
            {
                var sh = showings[i];
                var ar = TryParseAge(sh.age_rating);
                if (ar != null && ar.Value >= 15)
                {
                    any = true;
                    sb.AppendLine($"- #{(sh.id)} — {(string)sh.film_title} — {sh.start_time} — åldersgräns: {ar}");
                }
            }

            if (!any) sb.AppendLine("- inga visningar matchade (age_rating >= 15).");
            sb.AppendLine();
        }
    }

    // -----------------------------
    // config + filer
    // -----------------------------
    private static void LoadConfig()
    {
        try
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "db-config.json");
            var configJson = File.ReadAllText(configPath);
            var config = JSON.Parse(configJson);

            if (config.aiAccessToken != null)
                aiAccessToken = (string)config.aiAccessToken;
            else
                Log("warning: aiAccessToken not found in db-config.json!");
        }
        catch (Exception ex)
        {
            Log("error loading ai access token from config:", ex.Message);
        }
    }

    private static void LoadSystemPrompt()
    {
        try
        {
            var promptPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "system-prompt.md");
            if (File.Exists(promptPath))
            {
                systemPrompt = File.ReadAllText(promptPath);
                Log("loaded system prompt from system-prompt.md");
            }
            else
            {
                Log("no system-prompt.md found, running without system prompt");
            }
        }
        catch (Exception ex)
        {
            Log("error loading system prompt:", ex.Message);
        }
    }

    private static void LoadCinemaFacts()
    {
        try
        {
            var factsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "cinema-facts.json");
            if (!File.Exists(factsPath))
            {
                Log("no cinema-facts.json found, ai will only use db facts for grounding.");
                cinemaFacts = null;
                return;
            }

            var json = File.ReadAllText(factsPath);
            cinemaFacts = JSON.Parse(json);
            Log("loaded cinema facts from cinema-facts.json");
        }
        catch (Exception ex)
        {
            Log("error loading cinema facts:", ex.Message);
            cinemaFacts = null;
        }
    }
}