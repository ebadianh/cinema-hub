using System.Text;
using System.Text.Json;

namespace WebApp;

public static class AiProxyService
{
    private static readonly string ProxyUrl = "https://ai-api.nodehill.com";
    private static readonly HttpClient HttpClient = new HttpClient();

    public static async Task<dynamic> ChatAsync(Arr messages)
    {
        AiConfigService.EnsureLoaded();

        var requestBody = Obj(new { messages });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{ProxyUrl}/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {AiConfigService.AiAccessToken}");
        request.Content = new StringContent(
            JSON.Stringify(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await HttpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"AI proxy error: {responseContent}");

        return JSON.Parse(responseContent);
    }

    public static async Task<string> GetAssistantTextAsync(Arr messages)
    {
        var data = await ChatAsync(messages);

        try
        {
            return (string)data.choices[0].message.content ?? "";
        }
        catch
        {
            return "";
        }
    }
}