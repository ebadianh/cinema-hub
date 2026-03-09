namespace WebApp;

public static class AiDateResolutionService
{
    public static void Resolve(AiIntentFilters filters)
    {
        if (filters == null) return;

        var mode = (filters.date_mode ?? "").Trim().ToLowerInvariant();
        var raw = (filters.specific_date ?? "").Trim().ToLowerInvariant();

        if (mode == "today" || mode == "tomorrow" || mode == "tonight" || mode == "specific_date")
            return;

        // explicit relative words
        if (raw == "idag")
        {
            filters.date_mode = "today";
            filters.specific_date = "";
            return;
        }

        if (raw == "imorgon" || raw == "imorn")
        {
            filters.date_mode = "tomorrow";
            filters.specific_date = "";
            return;
        }

        if (raw == "överimorgon")
        {
            filters.date_mode = "specific_date";
            filters.specific_date = DateTime.Today.AddDays(2).ToString("yyyy-MM-dd");
            return;
        }

        if (raw == "ikväll")
        {
            filters.date_mode = "tonight";
            filters.time_of_day = "evening";
            filters.specific_date = "";
            return;
        }

        if (raw == "i helgen")
        {
            var saturday = NextWeekday(DateTime.Today, DayOfWeek.Saturday, includeToday: true);
            var monday = saturday.AddDays(2);

            filters.date_mode = "range";
            filters.range_start = saturday.ToString("yyyy-MM-dd");
            filters.range_end = monday.ToString("yyyy-MM-dd");
            filters.specific_date = "";
            return;
        }

        // weekday parsing like "måndag nästa vecka"
        var weekday = ParseSwedishWeekday(raw);
        if (weekday != null)
        {
            var baseDate = DateTime.Today;
            bool nextWeek = raw.Contains("nästa vecka");

            var target = NextWeekday(baseDate, weekday.Value, includeToday: true);

            if (nextWeek)
            {
                // move base date to next week first, then resolve weekday
                var nextWeekBase = StartOfWeek(baseDate).AddDays(7);
                target = NextWeekday(nextWeekBase, weekday.Value, includeToday: true);
            }

            filters.date_mode = "specific_date";
            filters.specific_date = target.ToString("yyyy-MM-dd");
            return;
        }

        if (string.IsNullOrWhiteSpace(filters.date_mode))
            filters.date_mode = "upcoming";
    }

    public static (DateTime? from, DateTime? to) GetRange(AiIntentFilters filters)
    {
        if (filters == null) return (DateTime.Today, DateTime.Today.AddDays(14));

        var mode = (filters.date_mode ?? "").Trim().ToLowerInvariant();

        if (mode == "today")
            return (DateTime.Today, DateTime.Today.AddDays(1));

        if (mode == "tomorrow")
        {
            var d = DateTime.Today.AddDays(1);
            return (d, d.AddDays(1));
        }

        if (mode == "tonight")
            return (DateTime.Today.AddHours(17), DateTime.Today.AddDays(1));

        if (mode == "specific_date" && !string.IsNullOrWhiteSpace(filters.specific_date))
        {
            DateTime date;
            if (DateTime.TryParse(filters.specific_date, out date))
                return (date.Date, date.Date.AddDays(1));
        }

        if (mode == "range" &&
            !string.IsNullOrWhiteSpace(filters.range_start) &&
            !string.IsNullOrWhiteSpace(filters.range_end))
        {
            DateTime start;
            DateTime end;
            if (DateTime.TryParse(filters.range_start, out start) &&
                DateTime.TryParse(filters.range_end, out end))
            {
                return (start.Date, end.Date.AddDays(1));
            }
        }

        return (DateTime.Today, DateTime.Today.AddDays(14));
    }

    private static DayOfWeek? ParseSwedishWeekday(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        raw = raw.ToLowerInvariant();

        if (raw.Contains("måndag")) return DayOfWeek.Monday;
        if (raw.Contains("tisdag")) return DayOfWeek.Tuesday;
        if (raw.Contains("onsdag")) return DayOfWeek.Wednesday;
        if (raw.Contains("torsdag")) return DayOfWeek.Thursday;
        if (raw.Contains("fredag")) return DayOfWeek.Friday;
        if (raw.Contains("lördag")) return DayOfWeek.Saturday;
        if (raw.Contains("söndag")) return DayOfWeek.Sunday;

        return null;
    }

    private static DateTime NextWeekday(DateTime from, DayOfWeek day, bool includeToday)
    {
        var start = from.Date;
        int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;

        if (daysToAdd == 0 && !includeToday)
            daysToAdd = 7;

        return start.AddDays(daysToAdd);
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}