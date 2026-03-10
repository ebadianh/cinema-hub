namespace WebApp;

public static class AiConfigService
{
    private static bool _loaded = false;
    private static string _aiAccessToken = "";
    private static string _systemPrompt = "";
    private static dynamic _cinemaFacts = null;

    public static string AiAccessToken
    {
        get
        {
            EnsureLoaded();
            return _aiAccessToken;
        }
    }

    public static string SystemPrompt
    {
        get
        {
            EnsureLoaded();
            return _systemPrompt;
        }
    }

    public static dynamic CinemaFacts
    {
        get
        {
            EnsureLoaded();
            return _cinemaFacts;
        }
    }

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        LoadConfig();
        LoadSystemPrompt();
        LoadCinemaFacts();
    }

    private static void LoadConfig()
    {
        try
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "db-config.json");
            var configJson = File.ReadAllText(configPath);
            var config = JSON.Parse(configJson);

            if (config.aiAccessToken != null)
                _aiAccessToken = (string)config.aiAccessToken;
            else
                Log("warning: aiAccessToken not found in db-config.json!");
        }
        catch (Exception ex)
        {
            Log("error loading ai access token from config:", ex.Message);
        }
    }

    private static void LoadSystemPrompt()
    {
        try
        {
            var promptPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "system-prompt.md");
            if (File.Exists(promptPath))
            {
                _systemPrompt = File.ReadAllText(promptPath);
                Log("loaded system prompt from system-prompt.md");
            }
            else
            {
                Log("no system-prompt.md found, running without system prompt");
            }
        }
        catch (Exception ex)
        {
            Log("error loading system prompt:", ex.Message);
        }
    }

    private static void LoadCinemaFacts()
    {
        try
        {
            var factsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "cinema-facts.json");
            if (!File.Exists(factsPath))
            {
                Log("no cinema-facts.json found, ai will only use db facts.");
                _cinemaFacts = null;
                return;
            }

            var json = File.ReadAllText(factsPath);
            _cinemaFacts = JSON.Parse(json);
            Log("loaded cinema facts from cinema-facts.json");
        }
        catch (Exception ex)
        {
            Log("error loading cinema facts:", ex.Message);
            _cinemaFacts = null;
        }
    }
}