import { useState, useCallback } from "react";

export default function useSeatLocking(showingId: string | undefined) {
  const [lockedByMe, setLockedByMe] = useState<Set<number>>(new Set());

  const lockSeats = useCallback(async (seatIds: number[]): Promise<boolean> => {
    if (!showingId) return false;

    // Optimistic update
    setLockedByMe(new Set(seatIds));

    try {
      const res = await fetch(`/api/showings/${showingId}/seats/lock`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ seatIds }),
      });

      if (!res.ok) {
        setLockedByMe(new Set());
        return false;
      }

      return true;
    } catch {
      setLockedByMe(new Set());
      return false;
    }
  }, [showingId]);

  const releaseLocks = useCallback(async () => {
    if (!showingId) return;

    try {
      await fetch(`/api/showings/${showingId}/seats/release`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({}),
      });
    } catch {
      // ignore
    }

    setLockedByMe(new Set());
  }, [showingId]);

  return { lockedByMe, lockSeats, releaseLocks };
}
