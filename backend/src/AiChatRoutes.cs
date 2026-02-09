namespace WebApp;

public static class AiChatRoutes
{
    private static string aiAccessToken = "";
    private static string systemPrompt = "";
    private static readonly string proxyUrl = "https://ai-api.nodehill.com";
    private static readonly HttpClient httpClient = new HttpClient();

    public static void Start()
    {
        // Load AI access token from config
        LoadConfig();

        // Load system prompt from markdown file
        LoadSystemPrompt();

        // POST /api/chat - Proxy chat requests to AI API with system prompt
        App.MapPost("/api/chat", async (HttpContext context, JsonElement bodyJson) =>
        {
            try
            {
                var body = JSON.Parse(bodyJson.ToString());
                var messages = (Arr)body.messages;

                if (messages == null)
                {
                    return RestResult.Parse(context, new { error = "Messages array is required." });
                }

                // Prepend system prompt if we have one
                var fullMessages = Arr();
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    fullMessages.Push(Obj(new { role = "system", content = systemPrompt }));
                }
                messages.ForEach(msg => fullMessages.Push(msg));

                // Create request payload
                var requestBody = Obj(new { messages = fullMessages });

                // Make request to AI API
                var request = new HttpRequestMessage(HttpMethod.Post, $"{proxyUrl}/v1/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {aiAccessToken}");
                request.Content = new StringContent(
                    JSON.Stringify(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = JSON.Parse(responseContent);
                    return RestResult.Parse(context, error);
                }

                var data = JSON.Parse(responseContent);
                return RestResult.Parse(context, data);
            }
            catch (Exception ex)
            {
                return RestResult.Parse(context, new { error = ex.Message });
            }
        });
    }

    private static void LoadConfig()
    {
        try
        {
            var configPath = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "db-config.json"
            );
            var configJson = File.ReadAllText(configPath);
            var config = JSON.Parse(configJson);

            if (config.aiAccessToken != null)
            {
                aiAccessToken = (string)config.aiAccessToken;
            }
            else
            {
                Log("WARNING: aiAccessToken not found in db-config.json!");
            }
        }
        catch (Exception ex)
        {
            Log("Error loading AI access token from config:", ex.Message);
        }
    }

    private static void LoadSystemPrompt()
    {
        try
        {
            var promptPath = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "system-prompt.md"
            );
            if (File.Exists(promptPath))
            {
                systemPrompt = File.ReadAllText(promptPath);
                Log("Loaded system prompt from system-prompt.md");
            }
            else
            {
                Log("No system-prompt.md found, running without system prompt");
            }
        }
        catch (Exception ex)
        {
            Log("Error loading system prompt:", ex.Message);
        }
    }
}
