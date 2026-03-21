namespace WebApp;

public static class AiOrchestratorService
{
    public static async Task<dynamic> HandleChatAsync(List<AiChatMessage> messages, HttpContext context)
    {
        AiConfigService.EnsureLoaded();

        var intent = await AiIntentService.ExtractIntentAsync(messages);
        var groundedFacts = BuildGroundedFacts(intent, context);
        var latestUserPrompt = messages.LastOrDefault(message => message.role == "user")?.content ?? "";
        var deterministicResponse = TryBuildDeterministicResponse(intent, groundedFacts, latestUserPrompt);

        if (deterministicResponse != null)
            return deterministicResponse;

        var fullMessages = Arr();

        if (!string.IsNullOrWhiteSpace(AiConfigService.SystemPrompt))
        {
            fullMessages.Push(Obj(new
            {
                role = "system",
                content = AiConfigService.SystemPrompt
            }));
        }

        fullMessages.Push(Obj(new
        {
            role = "system",
            content =
                """
                Du är Cinema-Bot för CinemaMob.
                                Personlighet: varm, tydlig, professionell och hjälpsam.
                                Ton: naturlig svenska, konkret, kort när det går men komplett när det behövs.

                Svara endast utifrån de grundade uppgifterna i "GROUNDING".

                Regler:
                - Hitta aldrig på specifika produkter, filmer, tider eller priser.
                                - Om fakta saknas i grounding: säg tydligt att informationen saknas.
                                - Gissa aldrig på saknad data. Var transparent med osäkerhet.
                - Om en användare frågar om en specifik snack-produkt och matched_item är null:
                  säg att du inte har exakt information om just den produkten.
                - Om grounding bara innehåller en kategori, som "läsk & mineralvatten",
                  får du inte nämna specifika märken eller varianter som Cola Zero eller Pepsi Max.
                - Om inga visningar hittas:
                                    använd no_results_message och erbjud en rimlig uppföljningsfråga.
                - Om frågan är utanför biografens område, använd out_of_scope_reply om den finns.
                - Om användaren vill boka:
                  förklara stegen, men påstå aldrig att bokningen redan är gjord.
                - Om needs_clarification är true och clarification_question finns:
                  ställ den frågan kort istället för att gissa.
                                - Om showings har film_url eller booking_url: inkludera klickbara markdown-länkar i svaret.
                                - När du länkar internt i CinemaMob: använd endast relativa länkar som börjar med "/".

                                Svarsmall:
                                - Vid listor: använd tydliga punktlistor.
                                - Vid tider och priser: återge exakta värden från grounding.
                                - Avsluta gärna med en kort följdfråga som hjälper användaren vidare.
                """.Trim()
        }));

        fullMessages.Push(Obj(new
        {
            role = "system",
            content = "## GROUNDING\n" + JSON.Stringify(groundedFacts)
        }));

        foreach (var chatMessage in messages)
        {
            fullMessages.Push(Obj(new
            {
                role = chatMessage.role,
                content = chatMessage.content
            }));
        }

        return await AiProxyService.ChatAsync(fullMessages);
    }

    private static dynamic TryBuildDeterministicResponse(AiIntentResult intent, dynamic groundedFacts, string latestUserPrompt)
    {
        // Use explicit fallback messages in high-risk cases where models often hallucinate.
        // This ensures the assistant stays aligned with verified grounding data.
        if (intent == null)
            return null;

        if (IsGreetingPrompt(latestUserPrompt))
        {
            return BuildAssistantResponse(
                "Hej! Jag hjälper gärna till med visningar, öppettider, priser, snacks och bokning. " +
                "Du kan till exempel fråga: \"vilka filmer går på lördag?\""
            );
        }

        if (intent.intent == "showings.search")
        {
            bool showingsFound = false;
            string noResultsMessage = "Jag hittar tyvärr inga visningar som matchar det du söker just nu.";
            string followUpSuggestion = "Vill du att jag testar ett bredare datumintervall eller en annan film?";

            try { showingsFound = groundedFacts.data.showings_found == true; } catch { }
            try { noResultsMessage = (string)groundedFacts.data.no_results_message ?? noResultsMessage; } catch { }
            try { followUpSuggestion = (string)groundedFacts.data.follow_up_suggestion ?? followUpSuggestion; } catch { }

            if (!showingsFound)
            {
                var hasAgeFilter = false;
                try
                {
                    hasAgeFilter = groundedFacts.filters.child_friendly == true ||
                                   groundedFacts.filters.age_rating_max != null;
                }
                catch
                {
                }

                if (hasAgeFilter || LooksLikeAgeSensitivePrompt(latestUserPrompt))
                {
                    return BuildAssistantResponse(
                        "Jag hittar tyvärr inga visningar som matchar åldersgränsen i din fråga just nu.\n\n" +
                        "Vill du att jag visar alla visningar på den dagen så kan vi jämföra åldersgränserna tillsammans?"
                    );
                }

                return BuildAssistantResponse($"{noResultsMessage}\n\n{followUpSuggestion}");
            }
        }

        if (intent.intent == "snacks.price")
        {
            bool snackItemFound = false;
            string snackItemRequested = "";
            dynamic matchedItem = null;

            try { snackItemFound = groundedFacts.data.snack_item_found == true; } catch { }
            try { snackItemRequested = (string)groundedFacts.data.snack_item_requested ?? ""; } catch { }
            try { matchedItem = groundedFacts.data.matched_item; } catch { }

            if (string.IsNullOrWhiteSpace(snackItemRequested))
                return BuildAssistantResponse(BuildSnackMenuResponseText(groundedFacts));

            if (snackItemFound)
            {
                var matchedName = snackItemRequested;
                var matchedPrice = "";

                try { matchedName = (string)matchedItem.name ?? matchedName; } catch { }
                try { matchedPrice = FormatPrice(matchedItem.price); } catch { }

                if (!string.IsNullOrWhiteSpace(matchedPrice))
                {
                    return BuildAssistantResponse(
                        $"{matchedName} kostar {matchedPrice} kr i vår kiosk.\n\n" +
                        "Vill du även se resten av snacksmenyn med priser?"
                    );
                }
            }

            if (!snackItemFound)
            {
                return BuildAssistantResponse(
                    $"Jag har ingen exakt information om just \"{snackItemRequested}\" i vårt kioskutbud. " +
                    "Jag kan däremot visa de snacks och drycker som finns i menyn om du vill."
                );
            }
        }

        if (intent.intent == "snacks.menu")
            return BuildAssistantResponse(BuildSnackMenuResponseText(groundedFacts));

        if (intent.intent == "general.capabilities")
        {
            if (LooksLikeComprehensiveInfoPrompt(latestUserPrompt))
                return BuildAssistantResponse(BuildComprehensiveInfoResponseText(groundedFacts));

            return BuildAssistantResponse(
                "Jag kan hjälpa dig med:\n" +
                "- öppettider\n" +
                "- biljettpriser\n" +
                "- kiosk/snacks\n" +
                "- hur man bokar biljett\n" +
                "- salongernas storlek\n" +
                "- vilka filmer som visas och när\n\n" +
                "Säg gärna vad du vill veta först."
            );
        }

        return null;
    }

    private static bool IsGreetingPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var normalizedPrompt = prompt
            .Trim()
            .ToLowerInvariant()
            .Replace("!", "")
            .Replace("?", "")
            .Replace(".", "")
            .Trim();

        return normalizedPrompt == "hej" ||
               normalizedPrompt == "hejsan" ||
               normalizedPrompt == "hej hej" ||
               normalizedPrompt == "tjena" ||
               normalizedPrompt == "tjenare" ||
               normalizedPrompt == "hallå" ||
               normalizedPrompt == "halloj" ||
               normalizedPrompt == "hello" ||
               normalizedPrompt == "hi";
    }

    private static bool LooksLikeAgeSensitivePrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var normalizedPrompt = prompt.ToLowerInvariant();
        return System.Text.RegularExpressions.Regex.IsMatch(normalizedPrompt, "\\b\\d{1,2}\\s*år\\b") ||
               normalizedPrompt.Contains("barnvänlig") ||
               normalizedPrompt.Contains("barnvänliga") ||
               normalizedPrompt.Contains("barntillåten") ||
               normalizedPrompt.Contains("för barn") ||
               normalizedPrompt.Contains("min son") ||
               normalizedPrompt.Contains("min dotter");
    }

    private static bool LooksLikeComprehensiveInfoPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return false;

        var normalizedPrompt = prompt.ToLowerInvariant();

        var explicitSummaryAsk = normalizedPrompt.Contains("all denna info") ||
                                 normalizedPrompt.Contains("all den här infon") ||
                                 normalizedPrompt.Contains("all den har infon") ||
                                 normalizedPrompt.Contains("sammanställ") ||
                                 normalizedPrompt.Contains("sammanfatta") ||
                                 normalizedPrompt.Contains("ge mig all") ||
                                 normalizedPrompt.Contains("som användare vill jag kunna prata med en ai-assistent");

        var topicSignals = 0;
        if (normalizedPrompt.Contains("öppettid") || normalizedPrompt.Contains("öppet")) topicSignals++;
        if (normalizedPrompt.Contains("biljett") || normalizedPrompt.Contains("pris")) topicSignals++;
        if (normalizedPrompt.Contains("snack") || normalizedPrompt.Contains("kiosk") || normalizedPrompt.Contains("popcorn")) topicSignals++;
        if (normalizedPrompt.Contains("boka") || normalizedPrompt.Contains("bokning")) topicSignals++;
        if (normalizedPrompt.Contains("salong") || normalizedPrompt.Contains("storlek")) topicSignals++;
        if (normalizedPrompt.Contains("filmer") || normalizedPrompt.Contains("visningar")) topicSignals++;
        if (normalizedPrompt.Contains("vad står") || normalizedPrompt.Contains("inriktning") || normalizedPrompt.Contains("koncept")) topicSignals++;

        return explicitSummaryAsk || topicSignals >= 4;
    }

    private static string BuildSnackMenuResponseText(dynamic groundedFacts)
    {
        dynamic snackMenu = null;
        try { snackMenu = groundedFacts.data.snack_menu; } catch { }

        var lines = new List<string>
        {
            "Vår kiosk har följande snacks och drycker med priser:"
        };

        AppendSnackGroupLines(lines, snackMenu, "classics", "Snacks");
        AppendSnackGroupLines(lines, snackMenu, "drinks", "Drycker");
        AppendSnackGroupLines(lines, snackMenu, "premium", "Premium");

        lines.Add("Vill du att jag rekommenderar något till en specifik film?");

        return string.Join("\n", lines);
    }

    private static void AppendSnackGroupLines(List<string> lines, dynamic snackMenu, string groupName, string heading)
    {
        lines.Add("");
        lines.Add($"**{heading}**");

        var foundInGroup = false;

        try
        {
            var items = snackMenu[groupName];

            foreach (var item in items)
            {
                string name = "";
                string priceText = "";

                try { name = (string)item.name ?? ""; } catch { }
                try { priceText = FormatPrice(item.price); } catch { }

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (string.IsNullOrWhiteSpace(priceText))
                    lines.Add($"- {name}");
                else
                    lines.Add($"- {name}: {priceText} kr");

                foundInGroup = true;
            }
        }
        catch
        {
        }

        if (!foundInGroup)
            lines.Add("- Information saknas just nu.");
    }

    private static string BuildComprehensiveInfoResponseText(dynamic groundedFacts)
    {
        var lines = new List<string>
        {
            "Absolut! Här är en sammanställning av CinemaMob:",
            ""
        };

        AppendOpeningHoursSection(lines, groundedFacts);
        AppendTicketPricesSection(lines, groundedFacts);
        AppendSnackSection(lines, groundedFacts);
        AppendConceptSection(lines, groundedFacts);
        AppendBookingSection(lines, groundedFacts);
        AppendSalongsSection(lines, groundedFacts);
        AppendShowingsSection(lines, groundedFacts);

        lines.Add("");
        lines.Add("Vill du att jag också filtrerar visningarna till en specifik dag, till exempel imorgon?");

        return string.Join("\n", lines);
    }

    private static void AppendOpeningHoursSection(List<string> lines, dynamic groundedFacts)
    {
        lines.Add("**Öppettider**");

        dynamic openingHours = null;
        try { openingHours = groundedFacts.data.opening_hours; } catch { }

        if (openingHours == null)
        {
            lines.Add("- Information saknas just nu.");
            lines.Add("");
            return;
        }

        var monThu = "";
        var fri = "";
        var sat = "";
        var sun = "";

        try { monThu = (string)openingHours.monThu ?? ""; } catch { }
        try { fri = (string)openingHours.fri ?? ""; } catch { }
        try { sat = (string)openingHours.sat ?? ""; } catch { }
        try { sun = (string)openingHours.sun ?? ""; } catch { }

        if (!string.IsNullOrWhiteSpace(monThu)) lines.Add($"- Mån–Tors: {monThu}");
        if (!string.IsNullOrWhiteSpace(fri)) lines.Add($"- Fredag: {fri}");
        if (!string.IsNullOrWhiteSpace(sat)) lines.Add($"- Lördag: {sat}");
        if (!string.IsNullOrWhiteSpace(sun)) lines.Add($"- Söndag: {sun}");

        lines.Add("");
    }

    private static void AppendTicketPricesSection(List<string> lines, dynamic groundedFacts)
    {
        lines.Add("**Biljettpriser**");

        var hasRows = false;
        try
        {
            foreach (var ticket in groundedFacts.data.ticket_prices)
            {
                var name = "";
                var priceText = "";
                try { name = (string)ticket.name ?? ""; } catch { }
                try { priceText = FormatPrice(ticket.price); } catch { }

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (string.IsNullOrWhiteSpace(priceText))
                    lines.Add($"- {name}");
                else
                    lines.Add($"- {name}: {priceText} kr");

                hasRows = true;
            }
        }
        catch
        {
        }

        if (!hasRows)
            lines.Add("- Information saknas just nu.");

        lines.Add("");
    }

    private static void AppendSnackSection(List<string> lines, dynamic groundedFacts)
    {
        lines.Add("**Kiosk / snacks**");

        dynamic snackMenu = null;
        try { snackMenu = groundedFacts.data.snack_menu; } catch { }

        AppendSnackGroupLines(lines, snackMenu, "classics", "Snacks");
        AppendSnackGroupLines(lines, snackMenu, "drinks", "Drycker");
        AppendSnackGroupLines(lines, snackMenu, "premium", "Premium");
        lines.Add("");
    }

    private static void AppendConceptSection(List<string> lines, dynamic groundedFacts)
    {
        lines.Add("**Vad bio:n står för**");

        var concept = "";
        try { concept = (string)groundedFacts.data.concept ?? ""; } catch { }

        if (string.IsNullOrWhiteSpace(concept))
            lines.Add("- Information saknas just nu.");
        else
            lines.Add($"- {concept}");

        lines.Add("");
    }

    private static void AppendBookingSection(List<string> lines, dynamic groundedFacts)
    {
        lines.Add("**Hur man bokar biljett**");

        var hasSteps = false;
        try
        {
            var stepNumber = 1;
            foreach (var step in groundedFacts.data.booking.steps)
            {
                var stepText = step?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(stepText))
                    continue;

                lines.Add($"{stepNumber}. {stepText}");
                stepNumber++;
                hasSteps = true;
            }
        }
        catch
        {
        }

        if (!hasSteps)
            lines.Add("- Information saknas just nu.");

        lines.Add("");
    }

    private static void AppendSalongsSection(List<string> lines, dynamic groundedFacts)
    {
        lines.Add("**Salongernas storlek**");

        var hasRows = false;
        try
        {
            foreach (var salong in groundedFacts.data.salongs)
            {
                var name = "";
                var seats = "";

                try { name = (string)salong.name ?? ""; } catch { }
                try { seats = salong.seats?.ToString() ?? ""; } catch { }

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (string.IsNullOrWhiteSpace(seats))
                    lines.Add($"- {name}");
                else
                    lines.Add($"- {name}: {seats} platser");

                hasRows = true;
            }
        }
        catch
        {
        }

        if (!hasRows)
            lines.Add("- Information saknas just nu.");

        lines.Add("");
    }

    private static void AppendShowingsSection(List<string> lines, dynamic groundedFacts)
    {
        lines.Add("**Filmer som visas när (kommande)**");

        var hasRows = false;
        var shownCount = 0;

        try
        {
            foreach (var showing in groundedFacts.data.upcoming_showings)
            {
                if (shownCount >= 10)
                    break;

                var title = "";
                var startRaw = "";
                var language = "";
                var subtitle = "";
                var ageRating = "";
                var filmUrl = "";
                var bookingUrl = "";

                try { title = (string)showing.film_title ?? ""; } catch { }
                try { startRaw = showing.start_time?.ToString() ?? ""; } catch { }
                try { language = (string)showing.language ?? ""; } catch { }
                try { subtitle = (string)showing.subtitle ?? ""; } catch { }
                try { ageRating = showing.age_rating?.ToString() ?? ""; } catch { }
                try { filmUrl = (string)showing.film_url ?? ""; } catch { }
                try { bookingUrl = (string)showing.booking_url ?? ""; } catch { }

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(startRaw))
                    continue;

                var whenText = startRaw;
                if (DateTime.TryParse(startRaw, out var parsedStart))
                    whenText = parsedStart.ToString("yyyy-MM-dd HH:mm");

                var details = new List<string>();
                if (!string.IsNullOrWhiteSpace(language)) details.Add(language);
                if (!string.IsNullOrWhiteSpace(subtitle)) details.Add($"med {subtitle} undertexter");
                if (!string.IsNullOrWhiteSpace(ageRating)) details.Add($"ålder {ageRating} år");

                var detailsText = details.Count > 0 ? $" – {string.Join(", ", details)}" : "";

                var links = new List<string>();
                if (!string.IsNullOrWhiteSpace(filmUrl)) links.Add($"[Läs mer]({filmUrl})");
                if (!string.IsNullOrWhiteSpace(bookingUrl)) links.Add($"[Boka]({bookingUrl})");
                var linksText = links.Count > 0 ? $" ({string.Join(" | ", links)})" : "";

                lines.Add($"- {whenText}: {title}{detailsText}{linksText}");
                hasRows = true;
                shownCount++;
            }
        }
        catch
        {
        }

        if (!hasRows)
            lines.Add("- Information saknas just nu.");
    }

    private static string FormatPrice(dynamic rawPrice)
    {
        if (rawPrice == null)
            return "";

        string text = rawPrice.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return "";

        decimal parsedInvariant;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedInvariant))
            return parsedInvariant % 1 == 0 ? ((int)parsedInvariant).ToString() : parsedInvariant.ToString("0.##");

        decimal parsedCurrent;
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedCurrent))
            return parsedCurrent % 1 == 0 ? ((int)parsedCurrent).ToString() : parsedCurrent.ToString("0.##");

        return text;
    }

    private static dynamic BuildAssistantResponse(string assistantContent)
    {
        return Obj(new
        {
            choices = Arr(
                Obj(new
                {
                    message = Obj(new
                    {
                        role = "assistant",
                        content = assistantContent
                    })
                })
            )
        });
    }

    private static dynamic BuildGroundedFacts(AiIntentResult intent, HttpContext context)
    {
        if (intent == null) intent = new AiIntentResult();
        if (intent.filters == null) intent.filters = new AiIntentFilters();

        var payload = Obj(new
        {
            intent = intent.intent,
            confidence = intent.confidence,
            needs_clarification = intent.needs_clarification,
            clarification_question = intent.clarification_question,
            filters = intent.filters
        });

        switch (intent.intent)
        {
            case "showings.search":
                var showings = AiShowingsService.Search(intent.filters, context);
                payload.data = Obj(new
                {
                    showings = showings,
                    showings_found = showings != null && showings.Length > 0,
                    requested_scope = intent.filters,
                    no_results_message = "Jag hittar tyvärr inga visningar som matchar det du söker just nu.",
                    follow_up_suggestion = "Vill du att jag testar ett bredare datumintervall eller en annan film?"
                });
                break;

            case "pricing.ticket":
                payload.data = Obj(new
                {
                    ticket_prices = AiPricingService.GetTicketPrices(context)
                });
                break;

            case "snacks.menu":
                payload.data = Obj(new
                {
                    snack_menu = AiSnackService.GetSnackMenu(),
                    exact_products_only = true,
                    note = "Om en specifik produkt inte finns uttryckligen i snack_menu får den inte påstås finnas."
                });
                break;

            case "snacks.price":
                var matchedSnack = AiSnackService.FindSnackByName(intent.filters.snack_item);
                payload.data = Obj(new
                {
                    snack_menu = AiSnackService.GetSnackMenu(),
                    matched_item = matchedSnack,
                    snack_item_requested = intent.filters.snack_item,
                    snack_item_found = matchedSnack != null,
                    is_known_category = AiSnackService.IsKnownSnackCategory(intent.filters.snack_item),
                    is_specific_brand_request = AiSnackService.LooksLikeSpecificBrandRequest(intent.filters.snack_item),
                    exact_products_only = true,
                    note = "Om matched_item är null får modellen inte säga att den specifika produkten finns."
                });
                break;

            case "booking.help":
                payload.data = Obj(new
                {
                    booking = AiBookingService.GetBookingHelp()
                });
                break;

            case "salongs.info":
                payload.data = Obj(new
                {
                    salongs = AiCinemaInfoService.GetSalongs(context)
                });
                break;

            case "hours.info":
                payload.data = Obj(new
                {
                    opening_hours = AiCinemaInfoService.GetOpeningHours()
                });
                break;

            case "cinema.info":
                payload.data = Obj(new
                {
                    concept = AiCinemaInfoService.GetConcept(),
                    opening_hours = AiCinemaInfoService.GetOpeningHours()
                });
                break;

            case "general.capabilities":
                payload.data = Obj(new
                {
                    capabilities = Arr(
                        "öppettider",
                        "biljettpriser",
                        "kiosk/snacks",
                        "hur man bokar biljett",
                        "salongernas storlek",
                        "aktuella visningar"
                    ),
                    concept = AiCinemaInfoService.GetConcept(),
                    opening_hours = AiCinemaInfoService.GetOpeningHours(),
                    ticket_prices = AiPricingService.GetTicketPrices(context),
                    snack_menu = AiSnackService.GetSnackMenu(),
                    booking = AiBookingService.GetBookingHelp(),
                    salongs = AiCinemaInfoService.GetSalongs(context),
                    upcoming_showings = AiShowingsService.GetUpcoming(context),
                    summary_note = "Använd dessa fakta för att ge en komplett sammanställning när användaren ber om all info."
                });
                break;

            default:
                payload.data = Obj(new
                {
                    capabilities = Arr(
                        "öppettider",
                        "biljettpriser",
                        "kiosk/snacks",
                        "hur man bokar biljett",
                        "salongernas storlek",
                        "aktuella visningar"
                    ),
                    scope_note = "Cinema-Bot hjälper främst till med biografens information och funktioner.",
                    out_of_scope_reply = "Jag hjälper främst till med visningar, priser, öppettider, snacks, salonger och bokning på CinemaMob."
                });
                break;
        }

        return payload;
    }
}