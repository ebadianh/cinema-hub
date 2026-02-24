import type { Seat, SelectedSeat, TicketType, SeatStatus } from "../interfaces/Booking";

export function calculateTotalTickets(ticketCounts: Record<number, number>): number {
  return Object.values(ticketCounts).reduce((sum, c) => sum + c, 0);
}

export function calculateTotalPrice(
  ticketCounts: Record<number, number>,
  ticketTypes: TicketType[]
): number {
  return ticketTypes.reduce((sum, type) => {
    const count = ticketCounts[type.id] || 0;
    return sum + count * type.price;
  }, 0);
}

export function generateTicketSummary(
  selectedSeats: SelectedSeat[]
): Record<string, { count: number; price: number }> {
  return selectedSeats.reduce((acc, { ticketType }) => {
    if (!acc[ticketType.name]) {
      acc[ticketType.name] = { count: 0, price: ticketType.price };
    }
    acc[ticketType.name].count++;
    return acc;
  }, {} as Record<string, { count: number; price: number }>);
}

export function formatSeatList(
  selectedSeats: SelectedSeat[],
  format: "short" | "long"
): string {
  const sorted = [...selectedSeats].sort((a, b) => {
    if (a.seat.row_num !== b.seat.row_num) return a.seat.row_num - b.seat.row_num;
    return a.seat.seat_number - b.seat.seat_number;
  });

  if (format === "short") {
    return sorted.map(s => `${s.seat.row_num}:${s.seat.seat_number}`).join(", ");
  }
  return sorted
    .map(s => `Rad ${s.seat.row_num}, Plats ${s.seat.seat_number}`)
    .join(" | ");
}

export function groupSeatsByRow(seats: Seat[]): Record<number, Seat[]> {
  const result: Record<number, Seat[]> = {};
  for (const seat of seats) {
    if (!result[seat.row_num]) result[seat.row_num] = [];
    result[seat.row_num].push(seat);
  }
  return result;
}

export function determineSeatStatus(
  seat: Seat,
  selectedSeats: SelectedSeat[],
  bookedSeatIds: Set<number>,
  lockedByMe: Set<number>,
  previewSeatIds: Set<number>
): SeatStatus {
  if (selectedSeats.some(s => s.seat.id === seat.id)) return "selected";
  if (bookedSeatIds.has(seat.id) && !lockedByMe.has(seat.id)) return "booked";
  if (previewSeatIds.has(seat.id)) return "preview";
  return "available";
}

export function formatShowtime(isoString: string): string {
  const date = new Date(isoString);
  return date.toLocaleString("sv-SE", {
    weekday: "short",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}
