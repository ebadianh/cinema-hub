using System.Text.Json;

namespace WebApp;

public static class AiChatRoutes
{
    public static void Start()
    {
        AiConfigService.EnsureLoaded();

        App.MapPost("/api/chat", async (HttpContext context, JsonElement bodyJson) =>
        {
            try
            {
                var body = JSON.Parse(bodyJson.ToString());
                var messagesArr = (Arr)body.messages;

                if (messagesArr == null)
                    return RestResult.Parse(context, new { error = "messages array is required." });

                var messages = ParseMessages(messagesArr);
                var result = await AiOrchestratorService.HandleChatAsync(messages, context);

                return RestResult.Parse(context, result);
            }
            catch (Exception ex)
            {
                return RestResult.Parse(context, new { error = ex.Message });
            }
        });

        App.MapGet("/api/debug/showings", (HttpContext context) =>
        {
            var rows = DbQuery.SQLQuery(@"
                SELECT id, film_title, salong_name, start_time, age_rating, genre
                FROM showings_detail
                ORDER BY start_time
                LIMIT 10
            ", null, context);

            return RestResult.Parse(context, new { count = rows?.Length ?? 0, rows });
        });
    }

    private static List<AiChatMessage> ParseMessages(Arr messagesArr)
    {
        var list = new List<AiChatMessage>();

        for (int i = 0; i < messagesArr.Length; i++)
        {
            try
            {
                var m = messagesArr[i];
                list.Add(new AiChatMessage
                {
                    role = (string)m.role ?? "",
                    content = (string)m.content ?? ""
                });
            }
            catch
            {
            }
        }

        return list;
    }
}