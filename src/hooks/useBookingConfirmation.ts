import { useState, useEffect } from "react";
import type { BookingConfirmationData } from "../interfaces/Booking";

export default function useBookingConfirmation(reference: string | undefined) {
  const [booking, setBooking] = useState<BookingConfirmationData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!reference) {
      setLoading(false);
      setError("Ingen bokningsreferens angiven");
      return;
    }

    const fetchBooking = async () => {
      try {
        setLoading(true);
        setError(null);

        const res = await fetch(`/api/booking_details?where=booking_reference=${reference}`);
        if (!res.ok) throw new Error("Kunde inte hämta bokningsdetaljer");

        const data = await res.json();
        if (!data || data.length === 0) {
          throw new Error("Bokningen hittades inte");
        }

        setBooking(data[0]);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Ett fel uppstod");
      } finally {
        setLoading(false);
      }
    };

    fetchBooking();
  }, [reference]);

  return { booking, loading, error };
}
