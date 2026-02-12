import type { TicketType, SelectedSeat } from '../../interfaces/Booking';

  interface TicketSelectorProps {
    selectedSeats: SelectedSeat[];
    ticketTypes: TicketType[];
    onTicketTypeChange: (seatId: number, ticketType: TicketType) => void;
  }

  export default function TicketSelector({ selectedSeats, ticketTypes, onTicketTypeChange }: TicketSelectorProps) {

    // Om inga säten är valda, visa meddelande
    if (selectedSeats.length === 0) {
      return (
        <div className="ch-booking-summary">
          <p className="text-muted text-center mb-0">
            Välj platser i salongen för att fortsätta
          </p>
        </div>
      );
    }

    // Annars visa valda säten
    return (
      <div className="ch-booking-summary">
        <h5 className="mb-3">Valda platser</h5>

        {selectedSeats.map(({ seat, ticketType }) => (
          <div key={seat.id} className="mb-3 pb-3 border-bottom">
            <div className="d-flex justify-content-between align-items-center mb-2">
              <span className="fw-semibold">
                Rad {seat.row_num}, Plats {seat.seat_number}
              </span>
              <span className="text-muted">{ticketType.price} kr</span>
            </div>

            <div className="ch-ticket-selector">
              {ticketTypes.map(type => (
                <button
                  key={type.id}
                  className={`btn ch-btn-outline ch-ticket-btn ${
                    ticketType.id === type.id ? 'ch-ticket-btn--active' : ''
                  }`}
                  onClick={() => onTicketTypeChange(seat.id, type)}
                >
                  {type.name} ({type.price} kr)
                </button>
              ))}
            </div>
          </div>
        ))}
      </div>
    );
  }
