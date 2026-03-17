namespace WebApp;

public static class AiOrchestratorService
{
    public static async Task<dynamic> HandleChatAsync(List<AiChatMessage> messages, HttpContext context)
    {
        AiConfigService.EnsureLoaded();

        var intent = await AiIntentService.ExtractIntentAsync(messages);
        var groundedFacts = BuildGroundedFacts(intent, context);
        var deterministicResponse = TryBuildDeterministicResponse(intent, groundedFacts);

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
                Du är Cinema-Bot för CinemaHub.
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

    private static dynamic TryBuildDeterministicResponse(AiIntentResult intent, dynamic groundedFacts)
    {
        // Use explicit fallback messages in high-risk cases where models often hallucinate.
        // This ensures the assistant stays aligned with verified grounding data.
        if (intent == null)
            return null;

        if (intent.intent == "showings.search")
        {
            bool showingsFound = false;
            string noResultsMessage = "Jag hittar tyvärr inga visningar som matchar det du söker just nu.";
            string followUpSuggestion = "Vill du att jag testar ett bredare datumintervall eller en annan film?";

            try { showingsFound = groundedFacts.data.showings_found == true; } catch { }
            try { noResultsMessage = (string)groundedFacts.data.no_results_message ?? noResultsMessage; } catch { }
            try { followUpSuggestion = (string)groundedFacts.data.follow_up_suggestion ?? followUpSuggestion; } catch { }

            if (!showingsFound)
                return BuildAssistantResponse($"{noResultsMessage}\n\n{followUpSuggestion}");
        }

        if (intent.intent == "snacks.price")
        {
            bool snackItemFound = false;
            string snackItemRequested = "produkten";

            try { snackItemFound = groundedFacts.data.snack_item_found == true; } catch { }
            try { snackItemRequested = (string)groundedFacts.data.snack_item_requested ?? snackItemRequested; } catch { }

            if (!snackItemFound)
            {
                return BuildAssistantResponse(
                    $"Jag har ingen exakt information om just \"{snackItemRequested}\" i vårt kioskutbud. " +
                    "Jag kan däremot visa de snacks och drycker som finns i menyn om du vill."
                );
            }
        }

        return null;
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
                    out_of_scope_reply = "Jag hjälper främst till med visningar, priser, öppettider, snacks, salonger och bokning på CinemaHub."
                });
                break;
        }

        return payload;
    }
}