import type { SelectedSeat, ShowingDetail, TicketType } from '../../interfaces/Booking';
import TicketCounter from './TicketCounter';
import { calculateTotalPrice, calculateTotalTickets, formatSeatList } from '../../utils/bookingUtils';

interface BookingSummaryProps {
  showing: ShowingDetail | null;
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
  const totalPrice = calculateTotalPrice(ticketCounts, ticketTypes);
  const totalTickets = calculateTotalTickets(ticketCounts);
  const hasSelectedSeats = selectedSeats.length > 0;
  const canSubmit = hasSelectedSeats && email.trim() !== '' && selectedSeats.length === totalTickets;

  return (
    <div className="ch-booking-right">
        <div className="ch-booking-summary mb-4">
          <TicketCounter
          ticketTypes={ticketTypes}
          ticketCounts={ticketCounts}
          onCountChange={onCountChange}
          maxAvailable={maxAvailable}
        />
        </div>
      <div className="ch-booking-summary">


        {hasSelectedSeats ? (
          <>
            <div className="ch-selected-seats-section">
              <h6>Valda platser</h6>
              <div className="ch-selected-seats-list">
                {formatSeatList(selectedSeats, 'short')}
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
