namespace WebApp;

public static class AiChatRoutes
{
    private static string aiAccessToken = "";
    private static string systemPrompt = "";
    private static dynamic cinemaFacts = null;

    private static readonly string proxyUrl = "https://ai-api.nodehill.com";
    private static readonly HttpClient httpClient = new HttpClient();

    public static void Start()
    {
        LoadConfig();
        LoadSystemPrompt();
        LoadCinemaFacts();

        // POST /api/chat - Proxy chat requests to AI API with system prompt + grounded cinema context
        App.MapPost("/api/chat", async (HttpContext context, JsonElement bodyJson) =>
        {
            try
            {
                var body = JSON.Parse(bodyJson.ToString());
                var messages = (Arr)body.messages;

                if (messages == null)
                    return RestResult.Parse(context, new { error = "Messages array is required." });

                var userText = ExtractLatestUserText(messages);

                // Build grounded context from DB + cinema-facts.json
                var cinemaContext = BuildCinemaContextMarkdown(userText, context);

                // Prepend system prompt + grounded context
                var fullMessages = Arr();

                if (!string.IsNullOrEmpty(systemPrompt))
                    fullMessages.Push(Obj(new { role = "system", content = systemPrompt }));

                if (!string.IsNullOrWhiteSpace(cinemaContext))
                {
                    fullMessages.Push(Obj(new
                    {
                        role = "system",
                        content =
                            "## CinemaHub fakta (källa)\n" +
                            "Använd endast fakta från denna sektion när du svarar.\n" +
                            "Om något saknas: säg att du inte vet och hänvisa användaren till rätt sida (/about eller startsidan).\n\n" +
                            cinemaContext
                    }));
                }

                // Add user chat history (keeps conversation context)
                for (int i = 0; i < messages.Length; i++)
                {
                    fullMessages.Push(messages[i]);
                }

                var requestBody = Obj(new { messages = fullMessages });

                var request = new HttpRequestMessage(HttpMethod.Post, $"{proxyUrl}/v1/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {aiAccessToken}");
                request.Content = new StringContent(
                    JSON.Stringify(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = JSON.Parse(responseContent);
                    return RestResult.Parse(context, error);
                }

                var data = JSON.Parse(responseContent);
                return RestResult.Parse(context, data);
            }
            catch (Exception ex)
            {
                return RestResult.Parse(context, new { error = ex.Message });
            }
        });

        App.MapGet("/api/debug/showings", (HttpContext context) =>
        {
            var rows = DbQuery.SQLQuery(@"
                SELECT id, film_title, salong_name, start_time
                FROM showings_detail
                ORDER BY start_time
                LIMIT 5
            ", null, context);
    
            return RestResult.Parse(context, new { count = rows?.Length ?? 0, rows });
        });
    }

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
                {
                    return (string)m.content ?? "";
                }
            }
            catch
            {
                // ignore malformed
            }
        }

        return "";
    }

    private static string BuildCinemaContextMarkdown(string userText, HttpContext context)
    {
        // Very lightweight intent detection 
        var t = (userText ?? "").ToLowerInvariant();

        bool wantsShowings =
            t.Contains("vilka filmer") || t.Contains("visas") || t.Contains("visning") || t.Contains("visningar") ||
            t.Contains("föreställ") || t.Contains("föreställning") ||
            t.Contains("idag") || t.Contains("imorgon") || LooksLikeDate(t);

        bool wantsPrices =
            t.Contains("pris") || t.Contains("kostar") || t.Contains("biljett") ||
            t.Contains("pension") || t.Contains("barn") || t.Contains("vuxen");

        bool wantsSalongs =
            t.Contains("salong") || t.Contains("platser") || t.Contains("stora") || t.Contains("lilla") || t.Contains("storlek");

        bool wantsHours =
            t.Contains("öppet") || t.Contains("öppettid") || t.Contains("öppettider");

        bool wantsSnacks =
            t.Contains("snack") || t.Contains("kiosk") || t.Contains("popcorn") || t.Contains("godis") || t.Contains("utbud");

        bool wantsBooking =
            t.Contains("boka") || t.Contains("bokar") || t.Contains("bokning") || (t.Contains("hur") && t.Contains("biljett"));

        // If user asks something very broad, include a bit of everything (still small)
        bool includeGeneral = !(wantsShowings || wantsPrices || wantsSalongs || wantsHours || wantsSnacks || wantsBooking);

        var sb = new System.Text.StringBuilder();

        // Opening hours + concept + snacks from cinema-facts.json (if present)
        if (cinemaFacts != null && (wantsHours || includeGeneral))
        {
            try
            {
                var oh = cinemaFacts.openingHours;
                if (oh != null)
                {
                    sb.AppendLine("### Öppettider");
                    sb.AppendLine($"- Mån–Tors: {oh.monThu}");
                    sb.AppendLine($"- Fre: {oh.fri}");
                    sb.AppendLine($"- Lör: {oh.sat}");
                    sb.AppendLine($"- Sön: {oh.sun}");
                    sb.AppendLine();
                }
            }
            catch { /* ignore */ }
        }

        if (cinemaFacts != null && (t.Contains("inrikt") || t.Contains("vision") || t.Contains("om") || includeGeneral))
        {
            try
            {
                var concept = (string)cinemaFacts.concept;
                if (!string.IsNullOrWhiteSpace(concept))
                {
                    sb.AppendLine("### Inriktning");
                    sb.AppendLine(concept.Trim());
                    sb.AppendLine();
                }
            }
            catch { /* ignore */ }
        }

        if (cinemaFacts != null && (wantsSnacks || includeGeneral))
        {
            try
            {
                var snacks = cinemaFacts.snacks;
                if (snacks != null)
                {
                    sb.AppendLine("### Kiosk / snacks-utbud");
                    AppendStringArray(sb, "Klassiker", snacks.classics);
                    AppendStringArray(sb, "Dryck", snacks.drinks);
                    AppendStringArray(sb, "Premium", snacks.premium);
                    sb.AppendLine();
                }
            }
            catch { /* ignore */ }
        }

        // Prices from DB
        if (wantsPrices || includeGeneral)
        {
            var prices = DbQuery.SQLQuery(
                "SELECT name, price FROM Ticket_Type ORDER BY price DESC",
                null,
                context
            );

            if (prices != null && prices.Length > 0)
            {
                sb.AppendLine("### Biljettpriser");
                for (int i = 0; i < prices.Length; i++)
                {
                    var p = prices[i];
                    sb.AppendLine($"- {(string)p.name}: {(p.price)} kr");
                }
                sb.AppendLine();
            }
        }

        // Salongs sizes from DB (count seats)
        if (wantsSalongs || includeGeneral)
        {
           var salongsSql = @"
                SELECT sa.id, sa.name, COUNT(se.id) AS seats
                FROM Salongs sa
                LEFT JOIN Seats se ON se.salong_id = sa.id
                GROUP BY sa.id, sa.name
                ORDER BY sa.id
                ".Trim();

            var salongs = DbQuery.SQLQuery(salongsSql, null, context);

            if (salongs != null && salongs.Length > 0)
            {
                sb.AppendLine("### Salonger (antal platser)");
                for (int i = 0; i < salongs.Length; i++)
                {
                    var s = salongs[i];
                    sb.AppendLine($"- {(string)s.name}: {(s.seats)} platser");
                }
                sb.AppendLine();
            }
            if (salongs.Length == 0)
            {
                sb.AppendLine("### Salonger (antal platser)");
                sb.AppendLine("- Inga salonger hittades i databasen.");
                sb.AppendLine();
            }
        }

        // Showings from DB (optionally date/salong filter)
        if (wantsShowings || includeGeneral)
        {
            // date filter
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

            // salong filter
            string salongLike = null;
            if (t.Contains("stora")) salongLike = "%Stora%";
            if (t.Contains("lilla")) salongLike = "%Lilla%";

            // film filter (very simple)
            string filmLike = TryExtractFilmQuery(t);

            var where = new System.Text.StringBuilder("WHERE 1=1 ");            var paramObj = Obj();

            if (from != null && to != null)
            {
                where.Clear();
                where.Append("WHERE start_time >= @from AND start_time < @to ");
            paramObj.from = from.Value.ToString("yyyy-MM-dd HH:mm:ss"); // fixat så att datum är hårt typat och inte dynamiskt
            paramObj.to   = to.Value.ToString("yyyy-MM-dd HH:mm:ss");
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
                        SELECT id, film_title, salong_name, start_time, language, subtitle
                        FROM showings_detail
                        {where}
                        ORDER BY start_time
                        LIMIT 12
                        ".Trim();

                    var showings = DbQuery.SQLQuery(showingsSql, paramObj, context);

                sb.AppendLine("### Aktuella visningar");
                if (showings != null && showings.Length > 0)
                {
                    for (int i = 0; i < showings.Length; i++)
                    {
                        var sh = showings[i];
                        sb.AppendLine($"- #{(sh.id)} — {(string)sh.film_title} — {(string)sh.salong_name} — {(sh.start_time)}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("**Bokningslänk:** använd `/booking/<visnings-id>` (t.ex. `/booking/123`).");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("- Inga visningar hittades med de filtren.");
                    sb.AppendLine();
                }
            }

            catch
            {
                sb.AppendLine("### Aktuella visningar");
                sb.AppendLine("- Kunde inte hämta visningar just nu.");
                sb.AppendLine();
            }

        }

        // Booking flow hint (not DB, but app behavior / routes)
        if (wantsBooking || includeGeneral)
        {
            sb.AppendLine("### Bokningsflöde (i appen)");
            sb.AppendLine("1) Gå till startsidan och välj en visning");
            sb.AppendLine("2) Du hamnar på filmdetaljsidan");
            sb.AppendLine("3) Välj platser, biljettyp och ange email");
            sb.AppendLine("4) Bekräfta → du kommer till en bekräftelsesida");
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static void AppendStringArray(System.Text.StringBuilder sb, string title, dynamic arr)
    {
        try
        {
            if (arr == null) return;

            sb.AppendLine($"- {title}:");
            foreach (var item in arr)
            {
                sb.AppendLine($"  - {item}");
            }
        }
        catch
        {
            // ignore
        }
    }

    private static bool LooksLikeDate(string t)
    {
        // naive: "2026-03-03" or "2026/03/03"
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
        var stop = new HashSet<string>(new[]
        {
            "visa","visas","visning","visningar","föreställning","föreställningar",
            "idag","imorgon","salong","stora","lilla","pris","biljett","bokning","boka",
            "öppettider","öppet","kiosk","snacks","film","filmer","när","vilka","som"
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
                Log("WARNING: aiAccessToken not found in db-config.json!");
        }
        catch (Exception ex)
        {
            Log("Error loading AI access token from config:", ex.Message);
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
                Log("Loaded system prompt from system-prompt.md");
            }
            else
            {
                Log("No system-prompt.md found, running without system prompt");
            }
        }
        catch (Exception ex)
        {
            Log("Error loading system prompt:", ex.Message);
        }
    }

    private static void LoadCinemaFacts()
    {
        try
        {
            var factsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "cinema-facts.json");
            if (!File.Exists(factsPath))
            {
                Log("No cinema-facts.json found, AI will only use DB facts for grounding.");
                cinemaFacts = null;
                return;
            }

            var json = File.ReadAllText(factsPath);
            cinemaFacts = JSON.Parse(json);
            Log("Loaded cinema facts from cinema-facts.json");
        }
        catch (Exception ex)
        {
            Log("Error loading cinema facts:", ex.Message);
            cinemaFacts = null;
        }
    }
}