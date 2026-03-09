namespace WebApp;

public static class AiCinemaInfoService
{
    public static dynamic GetOpeningHours()
    {
        var facts = AiConfigService.CinemaFacts;
        if (facts == null) return null;

        try
        {
            return facts.openingHours;
        }
        catch
        {
            return null;
        }
    }

    public static string? GetConcept()
    {
        var facts = AiConfigService.CinemaFacts;
        if (facts == null) return null;

        try
        {
            return (string)facts.concept;
        }
        catch
        {
            return null;
        }
    }

    public static Arr GetSalongs(HttpContext context)
    {
        var sql = @"
            SELECT sa.id, sa.name, COUNT(se.id) AS seats
            FROM Salongs sa
            LEFT JOIN Seats se ON se.salong_id = sa.id
            GROUP BY sa.id, sa.name
            ORDER BY sa.id
        ".Trim();

        return DbQuery.SQLQuery(sql, null, context);
    }
}