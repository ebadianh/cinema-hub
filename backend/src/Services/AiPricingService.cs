namespace WebApp;

public static class AiPricingService
{
    public static Arr GetTicketPrices(HttpContext context)
    {
        return DbQuery.SQLQuery(
            "SELECT name, price FROM Ticket_Type ORDER BY price DESC",
            null,
            context
        );
    }
}