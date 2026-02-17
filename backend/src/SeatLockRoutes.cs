namespace WebApp;

public static class SeatLockRoutes
{
    public static void Start()
    {
        // SSE stream endpoint
        App.MapGet("/api/showings/{showingId}/seats/stream", async (HttpContext context, int showingId) =>
        {
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            var connectionId = Guid.NewGuid().ToString();

            // Send initial state
            var unavailable = SeatLockManager.GetUnavailableSeatIds(showingId);
            var json = JsonSerializer.Serialize(new { unavailableSeatIds = unavailable.ToArray() });
            var initialMessage = $"data: {json}\n\n";
            var bytes = System.Text.Encoding.UTF8.GetBytes(initialMessage);
            await context.Response.Body.WriteAsync(bytes);
            await context.Response.Body.FlushAsync();

            // Register client
            SseManager.AddClient(connectionId, context.Response, showingId);

            // Keep connection open until client disconnects
            var tcs = new TaskCompletionSource();
            context.RequestAborted.Register(() => tcs.TrySetResult());
            await tcs.Task;

            SseManager.RemoveClient(connectionId);
        });

        // Lock seats endpoint
        App.MapPost("/api/showings/{showingId}/seats/lock", async (HttpContext context, int showingId) =>
        {
            var holderId = Session.GetSessionId(context);

            using var reader = new StreamReader(context.Request.Body);
            var bodyStr = await reader.ReadToEndAsync();
            var body = JsonSerializer.Deserialize<JsonElement>(bodyStr);

            var seatIds = body.GetProperty("seatIds").EnumerateArray()
                .Select(e => e.GetInt32()).ToArray();

            // Max 10 seats per session
            var existingLocks = SeatLockManager.GetLockCountForHolder(holderId);
            if (existingLocks + seatIds.Length > 10)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Max 10 seats can be locked per session" });
                return;
            }

            // Validate that seat_ids belong to the showing's salong
            var validSeats = SQLQuery(
                @"SELECT s.id FROM seats s
                  JOIN salongs sa ON s.salong_id = sa.id
                  JOIN showings sh ON sh.salong_id = sa.id
                  WHERE sh.id = @showingId AND s.id IN (" + string.Join(",", seatIds) + ")",
                new { showingId }
            );
            var validSeatIds = new HashSet<int>();
            foreach (var row in validSeats)
            {
                validSeatIds.Add((int)row.id);
            }
            var invalidIds = seatIds.Where(id => !validSeatIds.Contains(id)).ToArray();
            if (invalidIds.Length > 0)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid seat IDs for this showing", invalidSeatIds = invalidIds });
                return;
            }

            // Check DB for already booked seats
            var booked = SQLQuery(
                "SELECT seat_id FROM booked_seats WHERE showing_id = @showingId",
                new { showingId }
            );
            var bookedIds = new HashSet<int>();
            foreach (var row in booked)
            {
                bookedIds.Add((int)row.seat_id);
            }

            var conflictWithBooked = seatIds.Where(id => bookedIds.Contains(id)).ToArray();
            if (conflictWithBooked.Length > 0)
            {
                context.Response.StatusCode = 409;
                await context.Response.WriteAsJsonAsync(new { error = "Seats already booked", conflictSeatIds = conflictWithBooked });
                return;
            }

            var success = SeatLockManager.TryLockSeats(showingId, seatIds, holderId);

            if (success)
            {
                var unavailable = SeatLockManager.GetUnavailableSeatIds(showingId);
                SseManager.BroadcastToShowing(showingId, unavailable);
                context.Response.StatusCode = 200;
                await context.Response.WriteAsJsonAsync(new { ok = true });
            }
            else
            {
                context.Response.StatusCode = 409;
                var unavailable = SeatLockManager.GetUnavailableSeatIds(showingId);
                await context.Response.WriteAsJsonAsync(new { error = "Some seats are already locked", unavailableSeatIds = unavailable.ToArray() });
            }
        });

        // Release locks endpoint
        App.MapPost("/api/showings/{showingId}/seats/release", async (HttpContext context, int showingId) =>
        {
            var holderId = Session.GetSessionId(context);

            SeatLockManager.ReleaseLocks(holderId);

            var unavailable = SeatLockManager.GetUnavailableSeatIds(showingId);
            SseManager.BroadcastToShowing(showingId, unavailable);

            context.Response.StatusCode = 200;
            await context.Response.WriteAsJsonAsync(new { ok = true });
        });
    }
}
