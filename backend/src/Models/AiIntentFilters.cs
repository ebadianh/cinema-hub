namespace WebApp;

public class AiIntentFilters
{
    public string date_mode { get; set; } = "";       // today, tomorrow, tonight, specific_date, upcoming, range
    public string specific_date { get; set; } = "";
    public string range_start { get; set; } = "";
    public string range_end { get; set; } = "";
    public string time_of_day { get; set; } = "";
    public string film_title { get; set; } = "";
    public string salong_name { get; set; } = "";
    public string genre { get; set; } = "";
    public string snack_item { get; set; } = "";
    public string ticket_type { get; set; } = "";
    public bool? child_friendly { get; set; }
    public int? age_rating_min { get; set; }
    public int? age_rating_max { get; set; }
}