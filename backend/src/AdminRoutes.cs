using System.Text.Json;

namespace WebApp;

public static class AdminRoutes
{
    public static void Start()
    {
        App.MapGet("/api/admin/films", GetFilms);
        App.MapPost("/api/admin/films", CreateFilm);
        App.MapPut("/api/admin/films/{id:int}", UpdateFilm);
        App.MapDelete("/api/admin/films/{id:int}", DeleteFilm);
        App.MapPost("/api/admin/films/{id:int}/directors", AddDirectorsToFilm);
        App.MapPost("/api/admin/films/{id:int}/actors", AddActorsToFilm);
    }

    private static IResult GetFilms(HttpContext context)
    {
        var result = SQLQuery("SELECT * FROM Films ORDER BY id ASC", null, context);
        return RestResult.Parse(context, result);
    }

    private static IResult CreateFilm(HttpContext context, JsonElement bodyJson)
    {
        var body = JSON.Parse(bodyJson.ToString());

        // Säkerställ att admin inte kan sätta eget id
        body.Delete("id");

        var parsed = ReqBodyParse("films", body);

        var sql = @"
            INSERT INTO Films (
                title,
                description,
                duration_minutes,
                age_rating,
                genre,
                images,
                trailers
            )
            VALUES (
                @title,
                @description,
                @duration_minutes,
                @age_rating,
                @genre,
                @images,
                @trailers
            )
        ";

        var result = SQLQueryOne(sql, parsed.body, context);

        if (result.error != null)
        {
            return RestResult.Parse(context, result);
        }

        // Hämta det senast skapade film-ID:et från Films-tabellen
        var latestFilm = SQLQueryOne(@"
        SELECT id FROM Films 
        WHERE title = @title 
        ORDER BY id DESC 
        LIMIT 1
    ", new { title = parsed.body.title }, context);

        return RestResult.Parse(context, Obj(new
        {
            id = (long)latestFilm.id,
            message = "Film skapad"
        }));
    }

    private static IResult UpdateFilm(HttpContext context, int id, JsonElement bodyJson)
    {
        var body = JSON.Parse(bodyJson.ToString());

        // id ska aldrig kunna ändras från payloaden
        body.id = id;

        var parsed = ReqBodyParse("films", body);

        var sql = @"
            UPDATE Films
            SET
                title = @title,
                description = @description,
                duration_minutes = @duration_minutes,
                age_rating = @age_rating,
                genre = @genre,
                images = @images,
                trailers = @trailers
            WHERE id = @id
        ";

        var result = SQLQueryOne(sql, parsed.body, context);
        return RestResult.Parse(context, result);
    }

    private static IResult DeleteFilm(HttpContext context, int id)
    {
        var bookingCheck = SQLQueryOne(@"
            SELECT COUNT(*) AS count
            FROM Bookings b
            INNER JOIN Showings s ON b.showing_id = s.id
            WHERE s.film_id = @id
        ", new { id }, context);

        if ((long)bookingCheck.count > 0)
        {
            return RestResult.Parse(context, Obj(new
            {
                error = "Filmen kan inte tas bort eftersom det finns bokningar kopplade till den."
            }));
        }

        var result = SQLQueryOne("DELETE FROM Films WHERE id = @id", new { id }, context);
        return RestResult.Parse(context, result);
    }

    private static IResult AddDirectorsToFilm(HttpContext context, int id, JsonElement bodyJson)
    {
        var body = JSON.Parse(bodyJson.ToString());
        var directors = (Arr)body.directors;

        foreach (var directorName in directors)
        {
            SQLQuery(@"
            INSERT INTO Directors (film_id, name)
            VALUES (@film_id, @name)
        ", new { film_id = id, name = (string)directorName }, context);
        }

        return RestResult.Parse(context, Obj(new { message = "Directors added" }));
    }

    private static IResult AddActorsToFilm(HttpContext context, int id, JsonElement bodyJson)
    {
        var body = JSON.Parse(bodyJson.ToString());
        var actors = (Arr)body.actors;

        var roleOrder = 1;
        foreach (var actorName in actors)
        {
            SQLQuery(@"
            INSERT INTO Actors (film_id, name, role_order)
            VALUES (@film_id, @name, @role_order)
        ", new { film_id = id, name = (string)actorName, role_order = roleOrder }, context);
            roleOrder++;
        }

        return RestResult.Parse(context, Obj(new { message = "Actors added" }));
    }
}