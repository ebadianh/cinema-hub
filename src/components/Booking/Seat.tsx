import type { Seat as SeatType, SeatStatus } from '../../interfaces/Booking';

interface SeatProps {
  seat: SeatType;
  status: SeatStatus;
  onClick: () => void;
  onMouseEnter: () => void;
  onMouseLeave: () => void;
}

export default function Seat({ seat, status, onClick, onMouseEnter, onMouseLeave }: SeatProps) {
  return (
    <button
      className={`ch-seat ch-seat--${status}`}
      onClick={onClick}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      disabled={status === "booked"}
      title={`Rad ${seat.row_num}, Plats ${seat.seat_number}`}
    >
      {seat.seat_number}
    </button>
  );
}
