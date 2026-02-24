import { useState, useEffect } from "react";
import type { ShowingDetail, Seat, TicketType } from "../interfaces/Booking";
import type User from "../interfaces/Users";

export default function useBookingData(showingId: string | undefined) {
  const [user, setUser] = useState<User | null>(null);
  const [showing, setShowing] = useState<ShowingDetail | null>(null);
  const [seats, setSeats] = useState<Seat[]>([]);
  const [ticketTypes, setTicketTypes] = useState<TicketType[]>([]);
  const [defaultTicketCounts, setDefaultTicketCounts] = useState<Record<number, number>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Hämta användarens inloggningsstatus
  useEffect(() => {
    fetch("/api/login", { credentials: "include" })
      .then(res => res.json())
      .then(data => {
        if (!data.error) setUser(data);
      })
      .catch(console.error);
  }, []);

  // Hämta visningsdata
  useEffect(() => {
    if (!showingId) return;

    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);

        const showingRes = await fetch(`/api/showings_detail/${showingId}`);
        if (!showingRes.ok) throw new Error("Kunde inte hämta visning");
        const showingData = await showingRes.json();
        setShowing(showingData);

        const seatsRes = await fetch(`/api/seats?where=salong_id=${showingData.salong_id}`);
        if (!seatsRes.ok) throw new Error("Kunde inte hämta säten");
        setSeats(await seatsRes.json());

        const ticketRes = await fetch("/api/ticket_type");
        if (!ticketRes.ok) throw new Error("Kunde inte hämta biljetttyper");
        const ticketData: TicketType[] = await ticketRes.json();
        setTicketTypes(ticketData);

        // Default: 2 vuxenbiljetter
        const adult = ticketData.find(t => t.name.toLowerCase().includes("vuxen"));
        if (adult) {
          setDefaultTicketCounts({ [adult.id]: 2 });
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Ett fel uppstod");
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [showingId]);

  return { user, showing, seats, ticketTypes, defaultTicketCounts, loading, error };
}
