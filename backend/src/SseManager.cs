using System.Collections.Concurrent;

namespace WebApp;

public record SseClient(HttpResponse Response, int ShowingId);

public static class SseManager
{
    private static readonly ConcurrentDictionary<string, SseClient> _clients = new();

    public static void AddClient(string connectionId, HttpResponse response, int showingId)
    {
        _clients[connectionId] = new SseClient(response, showingId);
    }

    public static void RemoveClient(string connectionId)
    {
        _clients.TryRemove(connectionId, out _);
    }

    public static void BroadcastToShowing(int showingId, HashSet<int> unavailableSeatIds)
    {
        var json = JsonSerializer.Serialize(new { unavailableSeatIds = unavailableSeatIds.ToArray() });
        var message = $"data: {json}\n\n";

        var toRemove = new List<string>();

        foreach (var kv in _clients)
        {
            if (kv.Value.ShowingId != showingId) continue;

            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                kv.Value.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                kv.Value.Response.Body.FlushAsync();
            }
            catch
            {
                toRemove.Add(kv.Key);
            }
        }

        foreach (var id in toRemove)
        {
            _clients.TryRemove(id, out _);
        }
    }
}
