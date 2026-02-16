import type { SelectedSeat, Showings, TicketType } from '../../interfaces/Booking';
import TicketCounter from './TicketCounter';

interface BookingSummaryProps {
  showing: Showings | null;
  selectedSeats: SelectedSeat[];
  ticketTypes: TicketType[];
  ticketCounts: Record<number, number>;
  onCountChange: (ticketTypeId: number, count: number) => void;
  email: string;
  onEmailChange: (email: string) => void;
  onConfirm: () => void;
  isLoggedIn: boolean;
  loading: boolean;
  maxAvailable: number;
}

export default function BookingSummary({
  showing,
  selectedSeats,
  ticketTypes,
  ticketCounts,
  onCountChange,
  email,
  onEmailChange,
  onConfirm,
  isLoggedIn,
  loading,
  maxAvailable
}: BookingSummaryProps) {
  // Beräkna totalpris baserat på ticketCounts
  const totalPrice = ticketTypes.reduce((sum, type) => {
    const count = ticketCounts[type.id] || 0;
    return sum + (count * type.price);
  }, 0);

  const totalTickets = Object.values(ticketCounts).reduce((sum, c) => sum + c, 0);
  const hasSelectedSeats = selectedSeats.length > 0;
  const canSubmit = hasSelectedSeats && email.trim() !== '' && selectedSeats.length === totalTickets;

  // Formatera valda platser
  const formatSelectedSeats = () => {
    if (selectedSeats.length === 0) return null;

    const sorted = [...selectedSeats].sort((a, b) => {
      if (a.seat.row_num !== b.seat.row_num) {
        return a.seat.row_num - b.seat.row_num;
      }
      return a.seat.seat_number - b.seat.seat_number;
    });

    return sorted.map(s => `${s.seat.row_num}:${s.seat.seat_number}`).join(', ');
  };

  return (
    <div className="ch-booking-right">
      <div className="ch-booking-summary">
        <TicketCounter
          ticketTypes={ticketTypes}
          ticketCounts={ticketCounts}
          onCountChange={onCountChange}
          maxAvailable={maxAvailable}
        />

        <div className="ch-booking-divider"></div>

        {hasSelectedSeats ? (
          <>
            <div className="ch-selected-seats-section">
              <h6>Valda platser</h6>
              <div className="ch-selected-seats-list">
                {formatSelectedSeats()}
              </div>
            </div>

            <div className="ch-booking-divider"></div>

            <div className="ch-booking-price-section">
              {ticketTypes.map(type => {
                const count = ticketCounts[type.id] || 0;
                if (count === 0) return null;
                return (
                  <div key={type.id} className="ch-summary-row">
                    <span>{count}x {type.name}</span>
                    <span>{count * type.price} kr</span>
                  </div>
                );
              })}

              <div className="ch-summary-row ch-total-row">
                <span>Totalt</span>
                <span>{totalPrice} kr</span>
              </div>
            </div>

            <div className="ch-booking-divider"></div>

            <div className="ch-email-section">
              <label className="form-label">E-postadress</label>
              <input
                type="email"
                className="form-control"
                value={email}
                onChange={(e) => onEmailChange(e.target.value)}
                placeholder="din@email.se"
                disabled={isLoggedIn}
              />
            </div>

            <button
              className="btn ch-btn-primary w-100 mt-4 py-2 ch-confirm-btn"
              disabled={!canSubmit || loading}
              onClick={onConfirm}
            >
              {loading ? 'Bearbetar...' : `Fortsätt → ${totalPrice} kr`}
            </button>
          </>
        ) : (
          <div className="ch-no-selection">
            {totalTickets > 0 ? (
              <p>Klicka på en rad i säteskartan för att välja platser</p>
            ) : (
              <p>Välj antal biljetter ovan för att börja</p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
