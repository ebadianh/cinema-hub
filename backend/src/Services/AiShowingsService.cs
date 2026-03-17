using System.Text;

namespace WebApp;

public static class AiShowingsService
{
    public static Arr Search(AiIntentFilters filters, HttpContext context)
    {
        if (filters == null) filters = new AiIntentFilters();

        AiDateResolutionService.Resolve(filters);
        var range = AiDateResolutionService.GetRange(filters);

        var where = new StringBuilder("WHERE 1=1 ");
        dynamic paramObj = Obj();

        if (range.from != null && range.to != null)
        {
            where.Append("AND start_time >= @from AND start_time < @to ");
            paramObj.from = range.from.Value.ToString("yyyy-MM-dd HH:mm:ss");
            paramObj.to = range.to.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        if (!string.IsNullOrWhiteSpace(filters.salong_name))
        {
            where.Append("AND salong_name LIKE @salong ");
            paramObj.salong = $"%{filters.salong_name}%";
        }

        if (!string.IsNullOrWhiteSpace(filters.film_title))
        {
            where.Append("AND film_title LIKE @film ");
            paramObj.film = $"%{filters.film_title}%";
        }

        if (!string.IsNullOrWhiteSpace(filters.genre))
        {
            where.Append("AND genre LIKE @genre ");
            paramObj.genre = $"%{filters.genre}%";
        }

        if (filters.child_friendly == true && filters.age_rating_max == null)
        {
            where.Append("AND CAST(age_rating AS SIGNED) <= @ageMax ");
            paramObj.ageMax = 7;
        }

        if (filters.age_rating_min != null)
        {
            where.Append("AND CAST(age_rating AS SIGNED) >= @ageMin ");
            paramObj.ageMin = filters.age_rating_min.Value;
        }

        if (filters.age_rating_max != null)
        {
            where.Append("AND CAST(age_rating AS SIGNED) <= @ageMax2 ");
            paramObj.ageMax2 = filters.age_rating_max.Value;
        }

        var sql = $@"
            SELECT
                id,
                film_id,
                film_title,
                salong_name,
                start_time,
                language,
                subtitle,
                age_rating,
                genre,
                CONCAT('/films/', film_id) AS film_url,
                CONCAT('/booking/', id) AS booking_url
            FROM showings_detaila
            {where}
            ORDER BY start_time
            LIMIT 50
        ".Trim();

        var rows = DbQuery.SQLQuery(sql, paramObj, context);

        if (!string.IsNullOrWhiteSpace(filters.time_of_day))
            rows = FilterByTimeOfDay(rows, filters.time_of_day);

        return rows;
    }

    public static Arr GetUpcoming(HttpContext context, int days = 14)
    {
        dynamic filters = new AiIntentFilters
        {
            date_mode = "upcoming"
        };

        return Search(filters, context);
    }

    private static Arr FilterByTimeOfDay(Arr rows, string timeOfDay)
    {
        var filtered = Arr();

        foreach (var row in rows)
        {
            try
            {
                DateTime dt;
                if (!DateTime.TryParse(row.start_time?.ToString(), out dt))
                    continue;

                bool include = false;
                var tod = (timeOfDay ?? "").ToLowerInvariant();

                if (tod == "morning") include = dt.Hour < 12;
                else if (tod == "afternoon") include = dt.Hour >= 12 && dt.Hour < 17;
                else if (tod == "evening") include = dt.Hour >= 17 && dt.Hour < 22;
                else if (tod == "night") include = dt.Hour >= 22;
                else include = true;

                if (include) filtered.Push(row);
            }
            catch
            {
            }
        }

        return filtered;
    }
}