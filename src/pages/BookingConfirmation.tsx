import { useParams, useNavigate } from "react-router-dom";
import useBookingConfirmation from "../hooks/useBookingConfirmation";
import MovieInfoCard from "../components/Booking/MovieInfoCard";
import { formatShowtime } from "../utils/bookingUtils";

export default function BookingConfirmation() {
  const { reference } = useParams<{ reference: string }>();
  const navigate = useNavigate();
  const { booking, loading, error } = useBookingConfirmation(reference);

  if (loading) {
    return (
      <div className="ch-confirmation-page">
        <div className="text-center">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Laddar...</span>
          </div>
          <p className="mt-3 text-muted">Laddar bokningsdetaljer...</p>
        </div>
      </div>
    );
  }

  if (error || !booking) {
    return (
      <div className="ch-confirmation-page">
        <div className="ch-confirmation-card text-center">
          <div className="ch-error-icon mb-4">&#10005;</div>
          <h2 className="mb-3">Bokningen hittades inte</h2>
          <p className="text-muted mb-4">{error || "Ogiltig bokningsreferens"}</p>
          <button className="btn ch-btn-primary" onClick={() => navigate("/")}>
            Till startsidan
          </button>
        </div>
      </div>
    );
  }

  const ticketSummary = booking.seats.reduce((acc, seat) => {
    if (!acc[seat.ticket_type]) {
      acc[seat.ticket_type] = { count: 0, price: seat.ticket_price };
    }
    acc[seat.ticket_type].count++;
    return acc;
  }, {} as Record<string, { count: number; price: number }>);

  const totalPrice = booking.seats.reduce((sum, seat) => sum + seat.ticket_price, 0);

  const duration = booking.duration_minutes
    ? `${Math.floor(booking.duration_minutes / 60)}h ${booking.duration_minutes % 60}min`
    : undefined;

  return (
    <div className="ch-confirmation-page">
      <div className="ch-confirmation-card">
        <div className="text-center">
          <div className="ch-success-icon mb-4">&#10003;</div>
          <h2 className="mb-2">Bokningsbekräftelse</h2>
          <div className="ch-ref-code">{booking.booking_reference}</div>
        </div>

        <div className="ch-confirmation-section">
          <MovieInfoCard
            title={booking.film_title}
            posterUrl={booking.film_images?.[0]}
            duration={duration}
            genre={booking.genre}
            description={booking.film_description}
            showtime={formatShowtime(booking.start_time)}
            salongName={booking.salong_name}
          />
        </div>

        <div className="ch-confirmation-section">
          <h5>Platser</h5>
          <div className="ch-seat-badges">
            {[...booking.seats]
              .sort((a, b) => a.row_num !== b.row_num ? a.row_num - b.row_num : a.seat_number - b.seat_number)
              .map((seat, i) => (
                <span key={i} className="ch-seat-badge">
                  Rad {seat.row_num}, Plats {seat.seat_number}
                </span>
              ))}
          </div>
        </div>

        <div className="ch-confirmation-section">
          <h5>Biljetter</h5>
          {Object.entries(ticketSummary).map(([type, { count, price }]) => (
            <div key={type} className="ch-summary-row">
              <span>{count}x {type}</span>
              <span>{count * price} kr</span>
            </div>
          ))}
          <div className="ch-summary-row ch-total-row">
            <span>Totalt</span>
            <span>{totalPrice} kr</span>
          </div>
        </div>

        <div className="ch-confirmation-section">
          <div className="ch-summary-row">
            <span className="text-muted">E-post</span>
            <span>{booking.email}</span>
          </div>
          <div className="ch-summary-row">
            <span className="text-muted">Bokad</span>
            <span>{formatShowtime(booking.booked_at)}</span>
          </div>
        </div>

        <div className="ch-confirmation-section text-center">
          <button className="btn ch-btn-primary" onClick={() => navigate("/")}>
            Till startsidan
          </button>
        </div>
      </div>
    </div>
  );
}
