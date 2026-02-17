import { useState, useEffect, useRef } from "react";

export default function useSeatStream(showingId: string | undefined) {
  const [unavailableSeatIds, setUnavailableSeatIds] = useState<Set<number>>(new Set());
  const [isConnected, setIsConnected] = useState(false);
  const eventSourceRef = useRef<EventSource | null>(null);

  useEffect(() => {
    if (!showingId) return;

    const es = new EventSource(`/api/showings/${showingId}/seats/stream`);
    eventSourceRef.current = es;

    es.onopen = () => setIsConnected(true);

    es.onmessage = (event) => {
      const data = JSON.parse(event.data);
      setUnavailableSeatIds(new Set(data.unavailableSeatIds));
    };

    es.onerror = () => {
      setIsConnected(false);
    };

    return () => {
      es.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    };
  }, [showingId]);

  return { unavailableSeatIds, isConnected };
}
