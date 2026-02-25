import Seat from './Seat.tsx';
import type { Seat as SeatType, SelectedSeat } from '../../interfaces/Booking';
import { groupSeatsByRow, determineSeatStatus } from '../../utils/bookingUtils';

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
  const seatsByRow = groupSeatsByRow(seats);
  const rows = Object.keys(seatsByRow).map(Number).sort((a, b) => a - b);

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
                  status={determineSeatStatus(seat, selectedSeats, bookedSeatIds, lockedByMe, previewSeatIds)}
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
      <div className="ch-seat-toggle-container">

      <button
          className={`ch-seat-mode-toggle ${manualMode ? 'ch-seat-mode-toggle--active' : ''}`}
          onClick={onToggleMode}
          type="button"
          >
          {manualMode ? 'Välj sits' : 'Autovälj'}
        </button>
          </div>
    </div>
  );
}
