

export interface Showings {
  id: number;
  film_id: number;
  salong_id: number;
  start_time: string;
  language?: string;
  subtitle?: string;
  film_title?: string;
  film_description?: string;
  duration_minutes?: number;
  age_rating?: string;
  genre?: string;
  film_images?: string[];
  film_trailers?: string[];
  salong_name: string;
}

export interface Seat{
  id: number;
  salong_id: number;
  row_num: number;
  seat_number: number;
}

export interface TicketType{
  id: number;
  name: string;
  price: number;
}

export interface SelectedSeat{
  seat: Seat;
  ticketType: TicketType;
}

export type SeatStatus = "available" | "booked" | "selected" | "preview";