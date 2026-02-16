import type { TicketType } from '../../interfaces/Booking';

interface TicketCounterProps {
  ticketTypes: TicketType[];
  ticketCounts: Record<number, number>;
  onCountChange: (ticketTypeId: number, count: number) => void;
  maxAvailable: number;
}

export default function TicketCounter({
  ticketTypes,
  ticketCounts,
  onCountChange,
  maxAvailable
}: TicketCounterProps) {
  const totalTickets = Object.values(ticketCounts).reduce((sum, c) => sum + c, 0);

  const handleDecrease = (typeId: number) => {
    const current = ticketCounts[typeId] || 0;
    if (current > 0) {
      onCountChange(typeId, current - 1);
    }
  };

  const handleIncrease = (typeId: number) => {
    if (totalTickets >= maxAvailable) return;
    const current = ticketCounts[typeId] || 0;
    onCountChange(typeId, current + 1);
  };

  return (
    <div className="ch-ticket-counter">
      <h5 className="mb-3">Antal biljetter</h5>

      {ticketTypes.map(type => (
        <div key={type.id} className="ch-ticket-counter-row">
          <div className="ch-ticket-counter-info">
            <span className="ch-ticket-counter-name">{type.name}</span>
            <span className="ch-ticket-counter-price">{type.price} kr</span>
          </div>
          <div className="ch-ticket-counter-controls">
            <button
              className="ch-counter-btn"
              onClick={() => handleDecrease(type.id)}
              disabled={(ticketCounts[type.id] || 0) === 0}
              aria-label={`Minska antal ${type.name}`}
            >
              −
            </button>
            <span className="ch-counter-value">
              {ticketCounts[type.id] || 0}
            </span>
            <button
              className="ch-counter-btn"
              onClick={() => handleIncrease(type.id)}
              disabled={totalTickets >= maxAvailable}
              aria-label={`Öka antal ${type.name}`}
            >
              +
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
