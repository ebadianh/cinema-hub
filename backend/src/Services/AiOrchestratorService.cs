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

        Viktiga regler:
        - Hitta aldrig på specifika produkter, filmer, tider eller priser.
        - Om en användare frågar om en specifik snack-produkt och den inte finns som matched_item i grounding:
          säg att du inte har exakt information om just den produkten.
        - Om grounding bara säger en kategori, till exempel "läsk", får du inte påstå en specifik variant som Cola Zero.
        - Om inga visningar hittas:
          säg att du inte hittar några matchande visningar, inte att du saknar tillgång till data.
        - Om frågan är utanför biografens område:
          säg kort att du främst hjälper till med biografens information och funktioner.
        - Om användaren vill boka:
          förklara stegen, men påstå aldrig att bokningen redan är gjord.
        """.Trim()
}));

        fullMessages.Push(Obj(new
        {
            role = "system",
            content =
                "## GROUNDING\n" +
                JSON.Stringify(groundedFacts)
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
                    requested_scope = intent.filters
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
                    snack_menu = AiSnackService.GetSnackMenu()
                });
                break;

            case "snacks.price":
                payload.data = Obj(new
                {
                    snack_menu = AiSnackService.GetSnackMenu(),
                    matched_item = AiSnackService.FindSnackByName(intent.filters?.snack_item)
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
                    scope_note = "Cinema-Bot hjälper främst till med biografens information och funktioner."
                });
                break;
                    }

                    return payload;
    }
}