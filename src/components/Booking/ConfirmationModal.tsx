import type { SelectedSeat, Showings } from '../../interfaces/Booking';

interface ConfirmationModalProps {
  showing: Showings | null;
  selectedSeats: SelectedSeat[];
  email: string;
  onConfirm: () => void;
  onCancel: () => void;
  loading: boolean;
}

export default function ConfirmationModal({
  showing,
  selectedSeats,
  email,
  onConfirm,
  onCancel,
  loading
}: ConfirmationModalProps) {
  const totalPrice = selectedSeats.reduce(
    (sum, s) => sum + s.ticketType.price,
    0
  );

  const ticketSummary = selectedSeats.reduce((acc, { ticketType }) => {
    if (!acc[ticketType.name]) {
      acc[ticketType.name] = { count: 0, price: ticketType.price };
    }
    acc[ticketType.name].count++;
    return acc;
  }, {} as Record<string, { count: number; price: number }>);

  return (
    <div className="ch-modal-overlay" onClick={onCancel}>
      <div className="ch-modal" onClick={(e) => e.stopPropagation()}>
        <h4 className="mb-4">Bekräfta din bokning</h4>

        {showing && (
          <div className="mb-3 pb-3 border-bottom">
            <div className="fw-semibold">{showing.film_title}</div>
            <div className="text-muted small">
              {showing.start_time} | {showing.salong_name}
            </div>
          </div>
        )}

        <div className="mb-3">
          <strong>Platser:</strong>
          <div className="text-muted">
            {selectedSeats
              .map(s => `Rad ${s.seat.row_num}, Plats ${s.seat.seat_number}`)
              .join(' | ')}
          </div>
        </div>

        <div className="mb-3 pb-3 border-bottom">
          <strong>Biljetter:</strong>
          {Object.entries(ticketSummary).map(([name, { count, price }]) => (
            <div key={name} className="d-flex justify-content-between text-muted">
              <span>{count}x {name}</span>
              <span>{count * price} kr</span>
            </div>
          ))}
        </div>

        <div className="mb-3">
          <strong>E-post:</strong>
          <div className="text-muted">{email}</div>
        </div>

        <div className="ch-modal-total mb-4">
          <span>Totalt att betala</span>
          <span className="fw-bold">{totalPrice} kr</span>
        </div>

        <div className="d-flex gap-3">
          <button
            className="btn ch-btn-outline flex-fill"
            onClick={onCancel}
            disabled={loading}
          >
            Avbryt
          </button>
          <button
            className="btn ch-btn-primary flex-fill"
            onClick={onConfirm}
            disabled={loading}
          >
            {loading ? 'Bokar...' : 'Bekräfta'}
          </button>
        </div>
      </div>
    </div>
  );
}
