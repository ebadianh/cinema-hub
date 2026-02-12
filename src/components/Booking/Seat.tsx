import type { Seat as SeatType, SeatStatus } from '../../interfaces/Booking';

interface SeatProps {
  seat: SeatType;
  status: SeatStatus;
  onClick: () => void;
}

export default function Seat({ seat, status, onClick } : SeatProps) {

  const handleClick = () => {
    if (status !== "booked") {
      onClick()
    } 
  }
  return (
    <button
      className={`ch-seat ch-seat--${status}`}
      onClick={handleClick}
      disabled={status === "booked"}
      title={`Rad ${seat.row_num}, Plats ${seat.seat_number}`}
    >
      {seat.seat_number}
    </button>
  );

}