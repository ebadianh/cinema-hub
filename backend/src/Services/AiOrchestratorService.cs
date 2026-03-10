namespace WebApp;

public static class AiOrchestratorService
{
    public static async Task<dynamic> HandleChatAsync(List<AiChatMessage> messages, HttpContext context)
    {
        AiConfigService.EnsureLoaded();

        var intent = await AiIntentService.ExtractIntentAsync(messages);
        var groundedFacts = BuildGroundedFacts(intent, context);

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
                Du är Cinema-Bot för CinemaHub.

                Svara endast utifrån de grundade uppgifterna i "GROUNDING".

                Regler:
                - Hitta aldrig på specifika produkter, filmer, tider eller priser.
                - Om en användare frågar om en specifik snack-produkt och matched_item är null:
                  säg att du inte har exakt information om just den produkten.
                - Om grounding bara innehåller en kategori, som "läsk & mineralvatten",
                  får du inte nämna specifika märken eller varianter som Cola Zero eller Pepsi Max.
                - Om inga visningar hittas:
                  säg att du inte hittar några matchande visningar.
                - Om frågan är utanför biografens område, använd out_of_scope_reply om den finns.
                - Om användaren vill boka:
                  förklara stegen, men påstå aldrig att bokningen redan är gjord.
                - Om needs_clarification är true och clarification_question finns:
                  ställ den frågan kort istället för att gissa.
                """.Trim()
        }));

        fullMessages.Push(Obj(new
        {
            role = "system",
            content = "## GROUNDING\n" + JSON.Stringify(groundedFacts)
        }));

        foreach (var m in messages)
        {
            fullMessages.Push(Obj(new
            {
                role = m.role,
                content = m.content
            }));
        }

        return await AiProxyService.ChatAsync(fullMessages);
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
                    no_results_message = "Jag hittar inga matchande visningar med de filtren."
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
                    )
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
                    out_of_scope_reply = "Jag hjälper främst till med biografens visningar, priser, öppettider, snacks, salonger och bokning."
                });
                break;
        }

        return payload;
    }
}