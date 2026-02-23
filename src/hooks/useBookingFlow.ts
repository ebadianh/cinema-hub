import { useState, useEffect, useCallback } from "react";
import type { SelectedSeat } from "../interfaces/Booking";

export default function useBookingFlow(
  showingId: string | undefined,
  selectedSeats: SelectedSeat[],
  email: string,
  lockSeats: (seatIds: number[]) => Promise<boolean>,
  releaseLocks: () => Promise<void>
) {
  const [showModal, setShowModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [bookingConfirmed, setBookingConfirmed] = useState(false);
  const [bookingNumber, setBookingNumber] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleConfirmClick = useCallback(async () => {
    if (selectedSeats.length === 0 || !email.trim()) return;

    const seatIds = selectedSeats.map(s => s.seat.id);
    const success = await lockSeats(seatIds);

    if (success) {
      setShowModal(true);
    } else {
      setError("Några av de valda sätena är inte längre tillgängliga. Försök igen.");
    }
  }, [selectedSeats, email, lockSeats]);

  const handleSubmitBooking = useCallback(async () => {
    if (!showingId || selectedSeats.length === 0) return;

    try {
      setSubmitting(true);
      setError(null);

      const bookingData = {
        showing_id: parseInt(showingId),
        email,
        tickets: selectedSeats.map(s => ({
          seat_id: s.seat.id,
          ticket_type_id: s.ticketType.id,
        })),
      };

      const res = await fetch("/api/bookings", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(bookingData),
      });

      if (!res.ok) {
        const errorData = await res.json();
        throw new Error(errorData.message || "Bokningen misslyckades");
      }

      const result = await res.json();
      setBookingNumber(result.booking_number || result.id?.toString());
      setBookingConfirmed(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Ett fel uppstod vid bokning");
    } finally {
      setSubmitting(false);
    }
  }, [showingId, selectedSeats, email]);

  const closeModal = useCallback(() => {
    setShowModal(false);
    if (!bookingConfirmed) {
      releaseLocks();
    }
  }, [bookingConfirmed, releaseLocks]);

  // Cleanup: beforeunload sendBeacon + releaseLocks on unmount
  useEffect(() => {
    const handleBeforeUnload = () => {
      if (showingId) {
        navigator.sendBeacon(
          `/api/showings/${showingId}/seats/release`,
          JSON.stringify({})
        );
      }
    };
    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => {
      window.removeEventListener("beforeunload", handleBeforeUnload);
      releaseLocks();
    };
  }, [showingId, releaseLocks]);

  return {
    showModal,
    submitting,
    bookingConfirmed,
    bookingNumber,
    error,
    handleConfirmClick,
    handleSubmitBooking,
    closeModal,
  };
}
