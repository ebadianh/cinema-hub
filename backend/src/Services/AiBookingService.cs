namespace WebApp;

public static class AiBookingService
{
    public static dynamic GetBookingHelp()
    {
        return Obj(new
        {
            steps = Arr(
                "Gå till startsidan och välj en visning.",
                "Öppna bokningssidan: /booking/<visnings-id>.",
                "Välj platser.",
                "Välj biljettyp och fyll i email.",
                "Bekräfta i flödet i appen."
            ),
            bookingPattern = "/booking/<visnings-id>"
        });
    }
}