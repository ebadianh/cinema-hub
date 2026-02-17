import { useState, useRef, useCallback } from "react";

export default function useSeatLocking(showingId: string | undefined) {
  const [lockedByMe, setLockedByMe] = useState<Set<number>>(new Set());
  const holderIdRef = useRef(crypto.randomUUID());
  const holderId = holderIdRef.current;

  const lockSeats = useCallback(async (seatIds: number[]): Promise<boolean> => {
    if (!showingId) return false;

    // Optimistic update
    setLockedByMe(new Set(seatIds));

    try {
      const res = await fetch(`/api/showings/${showingId}/seats/lock`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ seatIds, holderId }),
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
  }, [showingId, holderId]);

  const releaseLocks = useCallback(async () => {
    if (!showingId) return;

    try {
      await fetch(`/api/showings/${showingId}/seats/release`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ holderId }),
      });
    } catch {
      // ignore
    }

    setLockedByMe(new Set());
  }, [showingId, holderId]);

  return { holderId, lockedByMe, lockSeats, releaseLocks };
}
