import { useState, useEffect } from "react";

export default function useSeatStream(showingId: string | undefined) {
  const [unavailableSeatIds, setUnavailableSeatIds] = useState<Set<number>>(new Set());

  useEffect(() => {
    if (!showingId) return;

    const es = new EventSource(`/api/showings/${showingId}/seats/stream`);

    es.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data);
        setUnavailableSeatIds(new Set(data.unavailableSeatIds));
      } catch {
        // Ignore malformed SSE messages
      }
    };

    return () => {
      es.close();
    };
  }, [showingId]);

  return { unavailableSeatIds };
}
