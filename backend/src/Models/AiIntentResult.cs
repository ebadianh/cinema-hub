namespace WebApp;

public class AiIntentResult
{
    public string intent { get; set; } = "unknown";
    public double confidence { get; set; } = 0;
    public bool needs_clarification { get; set; } = false;
    public string? clarification_question { get; set; }
    public AiIntentFilters filters { get; set; } = new AiIntentFilters();
}