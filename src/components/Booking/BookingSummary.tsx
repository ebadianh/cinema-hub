 import type { SelectedSeat, Showings } from '../../interfaces/Booking';

  interface BookingSummaryProps {
    showing: Showings | null;
    selectedSeats: SelectedSeat[];
    email: string;
    onEmailChange: (email: string) => void;
    onConfirm: () => void;
    isLoggedIn: boolean;
    loading: boolean;
  }

  export default function BookingSummary({
    showing,
    selectedSeats,
    email,
    onEmailChange,
    onConfirm,
    isLoggedIn,
    loading
  }: BookingSummaryProps) {

    // Beräkna totalpris
    const totalPrice = selectedSeats.reduce(
      (sum, s) => sum + s.ticketType.price,
      0
    );

    // Gruppera biljetter per typ
    const ticketSummary = selectedSeats.reduce((acc, { ticketType }) => {
      if (!acc[ticketType.name]) {
        acc[ticketType.name] = { count: 0, price: ticketType.price };
      }
      acc[ticketType.name].count++;
      return acc;
    }, {} as Record<string, { count: number; price: number }>);

    const canSubmit = selectedSeats.length > 0 && email.trim() !== '';

    return (
      <div className="ch-booking-summary">
        <h5 className="mb-3">Sammanfattning</h5>

        {showing && (
          <div className="mb-3 pb-3 border-bottom">
            <div className="fw-semibold">{showing.film_title}</div>
            <div className="text-muted small">
              {showing.start_time} | {showing.salong_name}
            </div>
          </div>
        )}

        {Object.entries(ticketSummary).map(([name, { count, price }]) => (
          <div key={name} className="ch-summary-row">
            <span>{count}x {name}</span>
            <span>{count * price} kr</span>
          </div>
        ))}

        {selectedSeats.length > 0 && (
          <div className="ch-summary-row ch-total-row">
            <span>Totalt</span>
            <span>{totalPrice} kr</span>
          </div>
        )}

        <div className="mt-4">
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
          className="btn ch-btn-primary w-100 mt-4 py-2"
          disabled={!canSubmit || loading}
          onClick={onConfirm}
        >
          {loading ? 'Bearbetar...' : `Bekräfta bokning - ${totalPrice} kr`}
        </button>
      </div>
    );
  }
