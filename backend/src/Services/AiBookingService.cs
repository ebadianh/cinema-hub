namespace WebApp;

public static class AiBookingService
{
    public static dynamic GetBookingHelp()
    {
        return Obj(new
        {
            steps = Arr(
                "Gå till filmer och öppna filmen du vill se.",
                "Välj en visningstid och öppna bokningssidan.",
                "Välj platser i salongen.",
                "Välj biljettyper och ange din e-postadress.",
                "Bekräfta bokningen i flödet och spara bokningsreferensen."
            ),
            bookingPattern = "/booking/<visnings-id>",
            bookingEntryPoints = Arr("/", "/films/<film-id>")
        });
    }
}