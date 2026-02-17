import Seat from './Seat';
import type { Seat as SeatType, SelectedSeat, SeatStatus } from '../../interfaces/Booking';

interface SeatMapProps {
  seats: SeatType[];
  bookedSeatIds: Set<number>;
  selectedSeats: SelectedSeat[];
  onSeatClick: (seat: SeatType) => void;
  manualMode: boolean;
  onToggleMode: () => void;
  previewSeatIds: Set<number>;
  onSeatHover: (seat: SeatType) => void;
  onSeatLeave: () => void;
  lockedByMe: Set<number>;
}

export default function SeatMap({
  seats,
  bookedSeatIds,
  selectedSeats,
  onSeatClick,
  manualMode,
  onToggleMode,
  previewSeatIds,
  onSeatHover,
  onSeatLeave,
  lockedByMe
}: SeatMapProps) {
  const seatsByRow: Record<number, SeatType[]> = {};
  seats.forEach(seat => {
    if (!seatsByRow[seat.row_num]) {
      seatsByRow[seat.row_num] = [];
    }
    seatsByRow[seat.row_num].push(seat);
  });

  const rows = Object.keys(seatsByRow).map(Number).sort((a, b) => a - b);

  const getSeatStatus = (seat: SeatType): SeatStatus => {
    if (selectedSeats.some(s => s.seat.id === seat.id)) return "selected";
    if (bookedSeatIds.has(seat.id) && !lockedByMe.has(seat.id)) return "booked";
    if (previewSeatIds.has(seat.id)) return "preview";
    return "available";
  };

  return (
    <div className="ch-seat-map">
        

      <div className="ch-screen-label">Bioduk</div>
      <div className="ch-screen"></div>

      {rows.map(rowNum => (
        <div key={rowNum} className="ch-seat-row">
          <span className="ch-row-label">{rowNum}</span>
          <div className="ch-seats-container">
            {seatsByRow[rowNum]
              .sort((a, b) => a.seat_number - b.seat_number)
              .map(seat => (
                <Seat
                  key={seat.id}
                  seat={seat}
                  status={getSeatStatus(seat)}
                  onClick={() => onSeatClick(seat)}
                  onMouseEnter={() => onSeatHover(seat)}
                  onMouseLeave={onSeatLeave}
                />
              ))}
          </div>
        </div>
      ))}

      <div className="ch-seat-legend">
        <div className="ch-legend-item">
          <div className="ch-legend-box ch-legend-box--available"></div>
          <span>Ledigt</span>
        </div>
        <div className="ch-legend-item">
          <div className="ch-legend-box ch-legend-box--selected"></div>
          <span>Valt</span>
        </div>
        <div className="ch-legend-item">
          <div className="ch-legend-box ch-legend-box--booked"></div>
          <span>Upptaget</span>
        </div>
        
      </div>
      <button
          className={`ch-seat-mode-toggle ${manualMode ? 'ch-seat-mode-toggle--active' : ''}`}
          onClick={onToggleMode}
          type="button"
        >
          {manualMode ? 'Individuellt val' : 'Automatiskt val'}
        </button>
    </div>
  );
}
