import { useState, useEffect, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import SeatMap from "../components/booking/SeatMap";
import BookingSummary from "../components/booking/BookingSummary";
import ConfirmationModal from "../components/booking/ConfirmationModal";
import MovieInfoCard from "../components/booking/MovieInfoCard";
import useSeatStream from "../hooks/useSeatStream";
import useSeatLocking from "../hooks/useSeatLocking";
import type { Showings, Seat, TicketType, SelectedSeat } from "../interfaces/Booking";
import type User from "../interfaces/Users";

export default function Booking() {
  const { showingId } = useParams<{ showingId: string }>();
  const navigate = useNavigate();

  // User state
  const [user, setUser] = useState<User | null>(null);

  // Data states
  const [showing, setShowing] = useState<Showings | null>(null);
  const [seats, setSeats] = useState<Seat[]>([]);
  const [ticketTypes, setTicketTypes] = useState<TicketType[]>([]);

  // Real-time seat availability via SSE
  const { unavailableSeatIds } = useSeatStream(showingId);
  const { lockedByMe, lockSeats, releaseLocks } = useSeatLocking(showingId);

  // Alias for backward compat within this component
  const bookedSeatIds = unavailableSeatIds;

  // Selection states - ny approach med ticketCounts
  const [ticketCounts, setTicketCounts] = useState<Record<number, number>>({});
  const [selectedSeats, setSelectedSeats] = useState<SelectedSeat[]>([]);
  const [previewSeatIds, setPreviewSeatIds] = useState<Set<number>>(new Set());
  const [email, setEmail] = useState("");

  // Lägesval
  const [manualMode, setManualMode] = useState(false);

  // UI states
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [bookingConfirmed, setBookingConfirmed] = useState(false);
  const [bookingNumber, setBookingNumber] = useState<string | null>(null);

  // Beräkna totalt antal biljetter
  const totalTickets = Object.values(ticketCounts).reduce((sum, c) => sum + c, 0);

  // Hämta användarens inloggningsstatus
  useEffect(() => {
    fetch('/api/login', { credentials: 'include' })
      .then(res => res.json())
      .then(data => {
        if (!data.error) {
          setUser(data);
          setEmail(data.email);
        }
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

        // Hämta visning med filmdetaljer
        const showingRes = await fetch(`/api/showings_detail/${showingId}`);
        if (!showingRes.ok) throw new Error("Kunde inte hämta visning");
        const showingData = await showingRes.json();
        setShowing(showingData);

        // Hämta alla säten för salongen
        const seatsRes = await fetch(`/api/seats?where=salong_id=${showingData.salong_id}`);
        if (!seatsRes.ok) throw new Error("Kunde inte hämta säten");
        const seatsData = await seatsRes.json();
        setSeats(seatsData);

        // Hämta biljetttyper
        const ticketRes = await fetch('/api/ticket_type');
        if (!ticketRes.ok) throw new Error("Kunde inte hämta biljetttyper");
        const ticketData = await ticketRes.json();
        setTicketTypes(ticketData);

      } catch (err) {
        setError(err instanceof Error ? err.message : "Ett fel uppstod");
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [showingId]);

  // Sätt default: 2 vuxenbiljetter
  useEffect(() => {
    if (ticketTypes.length === 0) return;
    const adult = ticketTypes.find(t => t.name.toLowerCase().includes('vuxen'));
    if (adult) {
      setTicketCounts(prev => {
        const totalExisting = Object.values(prev).reduce((sum, c) => sum + c, 0);
        if (totalExisting > 0) return prev;
        return { ...prev, [adult.id]: 2 };
      });
    }
  }, [ticketTypes]);

  // Handler för att ändra antal biljetter
  const handleCountChange = (ticketTypeId: number, count: number) => {
    setTicketCounts(prev => ({
      ...prev,
      [ticketTypeId]: count
    }));
    // Rensa valda säten när antal ändras
    setSelectedSeats([]);
  };

  // Tilldela biljetttyper till en lista säten
  const assignTicketTypes = (seatList: Seat[]): SelectedSeat[] => {
    const result: SelectedSeat[] = [];
    let seatIndex = 0;
    for (const type of ticketTypes) {
      const count = ticketCounts[type.id] || 0;
      for (let i = 0; i < count && seatIndex < seatList.length; i++) {
        result.push({ seat: seatList[seatIndex], ticketType: type });
        seatIndex++;
      }
    }
    return result;
  };

  // Standard-läge: Chebyshev-spiral från klickat säte
  // Avstånd = max(|rad_diff|, |plats_diff|) → närliggande rader prioriteras över avlägsna platser på samma rad
  const allocateFromSeat = (clicked: Seat): Seat[] => {
    const seatsByRow: Record<number, Seat[]> = {};
    seats.forEach(s => {
      if (!seatsByRow[s.row_num]) seatsByRow[s.row_num] = [];
      seatsByRow[s.row_num].push(s);
    });

    const rows = Object.keys(seatsByRow).map(Number).sort((a, b) => a - b);

    // Lookup: "rad-plats" → Seat (bara lediga)
    const seatLookup = new Map<string, Seat>();
    for (const r of rows) {
      for (const s of seatsByRow[r] || []) {
        if (!bookedSeatIds.has(s.id)) {
          seatLookup.set(`${r}-${s.seat_number}`, s);
        }
      }
    }

    // Spiralordning av rader: klickad, +1, -1, +2, -2, ...
    const clickedIdx = rows.indexOf(clicked.row_num);
    const spiralRows: number[] = [clicked.row_num];
    for (let ring = 1; ring < rows.length; ring++) {
      if (clickedIdx + ring < rows.length) spiralRows.push(rows[clickedIdx + ring]);
      if (clickedIdx - ring >= 0) spiralRows.push(rows[clickedIdx - ring]);
    }

    const maxSeatNum = Math.max(...seats.map(s => s.seat_number));
    const maxDist = Math.max(maxSeatNum, rows.length);
    const collected: Seat[] = [];
    const collectedIds = new Set<number>();
    let needed = totalTickets;

    // Chebyshev-ring d = 0, 1, 2, ...
    for (let d = 0; d <= maxDist && needed > 0; d++) {
      for (const r of spiralRows) {
        if (needed <= 0) break;
        const rowDist = Math.abs(r - clicked.row_num);
        if (rowDist > d) continue;

        if (rowDist === d) {
          // Rad precis på ringkanten → alla platspositioner center-d .. center+d
          for (let sd = 0; sd <= d && needed > 0; sd++) {
            const positions = sd === 0 ? [clicked.seat_number]
              : [clicked.seat_number + sd, clicked.seat_number - sd];
            for (const pos of positions) {
              if (needed <= 0) break;
              if (pos < 1 || pos > maxSeatNum) continue;
              const seat = seatLookup.get(`${r}-${pos}`);
              if (seat && !collectedIds.has(seat.id)) {
                collected.push(seat);
                collectedIds.add(seat.id);
                needed--;
              }
            }
          }
        } else {
          // Rad innanför ringen → bara kanterna (plats ±d)
          const positions = [clicked.seat_number + d, clicked.seat_number - d];
          for (const pos of positions) {
            if (needed <= 0) break;
            if (pos < 1 || pos > maxSeatNum) continue;
            const seat = seatLookup.get(`${r}-${pos}`);
            if (seat && !collectedIds.has(seat.id)) {
              collected.push(seat);
              collectedIds.add(seat.id);
              needed--;
            }
          }
        }
      }
    }

    return collected;
  };

  // Handler för sätesklick
  const handleSeatClick = (clicked: Seat) => {
    if (totalTickets === 0) return;

    if (manualMode) {
      // Individuellt läge: toggle enskilt säte
      const alreadySelected = selectedSeats.find(s => s.seat.id === clicked.id);
      if (alreadySelected) {
        // Ta bort och omfördela biljetttyper
        const remaining = selectedSeats.filter(s => s.seat.id !== clicked.id).map(s => s.seat);
        setSelectedSeats(assignTicketTypes(remaining));
      } else if (selectedSeats.length < totalTickets) {
        const newSeatList = [...selectedSeats.map(s => s.seat), clicked];
        setSelectedSeats(assignTicketTypes(newSeatList));
      }
    } else {
      // Standard-läge: allokera från klickat säte åt höger
      const allocated = allocateFromSeat(clicked);
      setSelectedSeats(assignTicketTypes(allocated));
    }
    setPreviewSeatIds(new Set());
  };

  const handleSeatHover = (seat: Seat) => {
    if (totalTickets === 0) return;
    if (bookedSeatIds.has(seat.id)) return;

    if (manualMode) {
      const alreadySelected = selectedSeats.some(s => s.seat.id === seat.id);
      if (!alreadySelected && selectedSeats.length < totalTickets) {
        setPreviewSeatIds(new Set([seat.id]));
      }
    } else {
      const allocated = allocateFromSeat(seat);
      setPreviewSeatIds(new Set(allocated.map(s => s.id)));
    }
  };

  const handleSeatLeave = () => {
    setPreviewSeatIds(new Set());
  };

  // Remove selected seats that become unavailable (locked/booked by others)
  useEffect(() => {
    setSelectedSeats(prev => {
      const filtered = prev.filter(
        s => !unavailableSeatIds.has(s.seat.id) || lockedByMe.has(s.seat.id)
      );
      if (filtered.length !== prev.length) {
        return filtered;
      }
      return prev;
    });
  }, [unavailableSeatIds, lockedByMe]);

  // Cleanup locks on unmount
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

  // Lås säten och öppna bekräftelsedialog
  const handleConfirmClick = async () => {
    if (selectedSeats.length === 0 || !email.trim()) return;

    const seatIds = selectedSeats.map(s => s.seat.id);
    const success = await lockSeats(seatIds);

    if (success) {
      setShowModal(true);
    } else {
      setError("Några av de valda sätena är inte längre tillgängliga. Försök igen.");
    }
  };

  // Skicka bokning till API
  const handleSubmitBooking = async () => {
    if (!showingId || selectedSeats.length === 0) return;

    try {
      setSubmitting(true);
      setError(null);

      const bookingData = {
        showing_id: parseInt(showingId),
        email: email,
        tickets: selectedSeats.map(s => ({
          seat_id: s.seat.id,
          ticket_type_id: s.ticketType.id
        }))
      };

      const res = await fetch('/api/bookings', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(bookingData)
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
  };

  // Visa laddningsindikator
  if (loading) {
    return (
      <div className="ch-booking-page ch-booking-loading">
        <div className="text-center">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Laddar...</span>
          </div>
          <p className="mt-3 text-muted">Laddar visning...</p>
        </div>
      </div>
    );
  }

  // Visa felmeddelande
  if (error && !showing) {
    return (
      <div className="ch-booking-page">
        <div className="ch-booking-error">
          <div className="alert alert-danger" role="alert">
            {error}
          </div>
          <button className="btn ch-btn-outline" onClick={() => navigate(-1)}>
            Gå tillbaka
          </button>
        </div>
      </div>
    );
  }

  // Visa bekräftelse efter lyckad bokning
  if (bookingConfirmed) {
    return (
      <div className="ch-booking-page ch-booking-confirmed-page">
        <div className="ch-booking-confirmed text-center">
          <div className="ch-success-icon mb-4">&#10003;</div>
          <h2 className="mb-3">Tack för din bokning!</h2>
          {bookingNumber && (
            <p className="lead mb-4">
              Ditt bokningsnummer: <strong>{bookingNumber}</strong>
            </p>
          )}
          <p className="text-muted mb-4">
            En bekräftelse har skickats till {email}
          </p>
          <button
            className="btn ch-btn-primary"
            onClick={() => navigate('/')}
          >
            Tillbaka till startsidan
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="ch-booking-page">
      {/* Vänster kolumn - 70% */}
      <div className="ch-booking-left">
        {/* Tillbaka-knapp */}
        <button
          className="btn ch-btn-outline ch-back-btn"
          onClick={() => navigate(-1)}
        >
          &larr; Tillbaka
        </button>

        {/* Filminfo */}
        {showing && (
          <MovieInfoCard
            title={showing.film_title || ""}
            posterUrl={showing.film_images?.[0]}
            duration={showing.duration_minutes ? `${Math.floor(showing.duration_minutes / 60)}h ${showing.duration_minutes % 60}min` : undefined}
            genre={showing.genre}
            description={showing.film_description}
            showtime={showing.start_time}
            salongName={showing.salong_name}
          />
        )}

        {error && (
          <div className="alert alert-danger mb-4" role="alert">
            {error}
          </div>
        )}

        {/* Säteskarta */}
        <SeatMap
          seats={seats}
          bookedSeatIds={bookedSeatIds}
          selectedSeats={selectedSeats}
          onSeatClick={handleSeatClick}
          manualMode={manualMode}
          onToggleMode={() => setManualMode(prev => !prev)}
          previewSeatIds={previewSeatIds}
          onSeatHover={handleSeatHover}
          onSeatLeave={handleSeatLeave}
          lockedByMe={lockedByMe}
        />
      </div>

      {/* Höger kolumn - 30% sticky */}
      <BookingSummary
        showing={showing}
        selectedSeats={selectedSeats}
        ticketTypes={ticketTypes}
        ticketCounts={ticketCounts}
        onCountChange={handleCountChange}
        email={email}
        onEmailChange={setEmail}
        onConfirm={handleConfirmClick}
        isLoggedIn={!!user}
        loading={submitting}
        maxAvailable={seats.length - bookedSeatIds.size}
      />

      {/* Bekräftelsedialog */}
      {showModal && (
        <ConfirmationModal
          showing={showing}
          selectedSeats={selectedSeats}
          email={email}
          onConfirm={handleSubmitBooking}
          onCancel={() => {
            setShowModal(false);
            if (bookingConfirmed) {
              navigate('/');
            } else {
              releaseLocks();
            }
          }}
          loading={submitting}
          bookingNumber={bookingNumber}
          error={error}
        />
      )}
    </div>
  );
}
