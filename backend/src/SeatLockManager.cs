using System.Collections.Concurrent;

namespace WebApp;

public record SeatLock(string HolderId, int ShowingId, DateTime ExpiresAt);

public static class SeatLockManager
{
    private static readonly ConcurrentDictionary<int, SeatLock> _locks = new();
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);
    private static Timer _cleanupTimer = null!;

    public static void Start()
    {
        _cleanupTimer = new Timer(CleanupExpiredLocks, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    public static bool TryLockSeats(int showingId, int[] seatIds, string holderId)
    {
        lock (_locks)
        {
            // Check all seats are available
            foreach (var seatId in seatIds)
            {
                if (_locks.TryGetValue(seatId, out var existing))
                {
                    if (existing.ShowingId == showingId && existing.HolderId != holderId && existing.ExpiresAt > DateTime.UtcNow)
                    {
                        return false;
                    }
                }
            }

            // Lock all seats
            var expiresAt = DateTime.UtcNow.Add(LockTimeout);
            foreach (var seatId in seatIds)
            {
                _locks[seatId] = new SeatLock(holderId, showingId, expiresAt);
            }
            return true;
        }
    }

    public static void ReleaseLocks(string holderId, int showingId)
    {
        var toRemove = _locks.Where(kv => kv.Value.HolderId == holderId && kv.Value.ShowingId == showingId).Select(kv => kv.Key).ToList();
        foreach (var key in toRemove)
        {
            _locks.TryRemove(key, out _);
        }
    }

    public static int GetLockCountForHolder(string holderId)
    {
        return _locks.Values.Count(l => l.HolderId == holderId && l.ExpiresAt > DateTime.UtcNow);
    }

    public static HashSet<int> GetLockedSeatIds(int showingId)
    {
        var now = DateTime.UtcNow;
        return _locks
            .Where(kv => kv.Value.ShowingId == showingId && kv.Value.ExpiresAt > now)
            .Select(kv => kv.Key)
            .ToHashSet();
    }

    public static HashSet<int> GetUnavailableSeatIds(int showingId)
    {
        // Locked seats
        var unavailable = GetLockedSeatIds(showingId);

        // Also add booked seats from DB
        var booked = SQLQuery(
            "SELECT seat_id FROM booked_seats WHERE showing_id = @showingId",
            new { showingId }
        );
        foreach (var row in booked)
        {
            unavailable.Add((int)row.seat_id);
        }

        return unavailable;
    }

    private static async void CleanupExpiredLocks(object state)
    {
        var now = DateTime.UtcNow;
        var expiredByShowing = new HashSet<int>();

        foreach (var kv in _locks)
        {
            if (kv.Value.ExpiresAt <= now)
            {
                if (_locks.TryRemove(kv.Key, out var removed))
                {
                    expiredByShowing.Add(removed.ShowingId);
                }
            }
        }

        // Broadcast update for affected showings
        foreach (var showingId in expiredByShowing)
        {
            var unavailable = GetUnavailableSeatIds(showingId);
            await SseManager.BroadcastToShowing(showingId, unavailable);
        }
    }
}
