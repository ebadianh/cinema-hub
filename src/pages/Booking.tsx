import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { QRCodeSVG } from "qrcode.react";
import SeatMap from "../components/Booking/SeatMap";
import BookingSummary from "../components/Booking/BookingSummary";
import ConfirmationModal from "../components/Booking/ConfirmationModal";
import MovieInfoCard from "../components/Booking/MovieInfoCard";
import useBookingData from "../hooks/useBookingData";
import useSeatStream from "../hooks/useSeatStream";
import useSeatLocking from "../hooks/useSeatLocking";
import useSeatSelection from "../hooks/useSeatSelection";
import useBookingFlow from "../hooks/useBookingFlow";

export default function Booking() {
  const { showingId } = useParams<{ showingId: string }>();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");

  const { user, showing, seats, ticketTypes, defaultTicketCounts, loading, error: dataError } = useBookingData(showingId);
  const { unavailableSeatIds } = useSeatStream(showingId);
  const { lockedByMe, lockSeats, releaseLocks } = useSeatLocking(showingId);
  const selection = useSeatSelection(seats, ticketTypes, unavailableSeatIds, lockedByMe, defaultTicketCounts);
  const flow = useBookingFlow(showingId, selection.selectedSeats, email, lockSeats, releaseLocks);

  // Set email from user data
  if (user && email === "") {
    setEmail(user.email);
  }

  if (loading) {
    return (
      <div className="ch-booking-page ch-booking-loading">
        <div className="text-center">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Laddar...</span>
          </div>
          <p className="mt-3 text-muted">Laddar visning...</p>
        </div>
      </div>
    );
  }

  if (dataError && !showing) {
    return (
      <div className="ch-booking-page">
        <div className="ch-booking-error">
          <div className="alert alert-danger" role="alert">
            {dataError}
          </div>
          <button className="btn ch-btn-outline" onClick={() => navigate(-1)}>
            Gå tillbaka
          </button>
        </div>
      </div>
    );
  }

  if (flow.bookingConfirmed) {
    return (
      <div className="ch-booking-page ch-booking-confirmed-page">
        <div className="ch-booking-confirmed text-center">
          <div className="ch-success-icon mb-4">&#10003;</div>
          <h2 className="mb-3">Tack för din bokning!</h2>
          {flow.bookingReference && (
            <>
              <p className="lead mb-4">
                Ditt bokningsnummer: <strong>{flow.bookingReference}</strong>
              </p>
              <QRCodeSVG value={flow.bookingReference} size={128} />
            </>
          )}
          <p className="text-muted mb-4">
            En bekräftelse har skickats till {email}
          </p>
          <button
            className="btn ch-btn-primary"
            onClick={() => navigate('/')}
          >
            Tillbaka till startsidan
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="ch-booking-page">
      <div className="ch-booking-left">

        {showing && (
          <MovieInfoCard
            title={showing.film_title || ""}
            posterUrl={showing.film_images?.[0]}
            duration={showing.duration_minutes ? `${Math.floor(showing.duration_minutes / 60)}h ${showing.duration_minutes % 60}min` : undefined}
            genre={showing.genre}
            description={showing.film_description}
            showtime={showing.start_time}
            salongName={showing.salong_name}
          />
        )}

        {flow.error && (
          <div className="alert alert-danger mb-4" role="alert">
            {flow.error}
          </div>
        )}

        <SeatMap
          seats={seats}
          bookedSeatIds={unavailableSeatIds}
          selectedSeats={selection.selectedSeats}
          onSeatClick={selection.handleSeatClick}
          manualMode={selection.manualMode}
          onToggleMode={() => selection.setManualMode(prev => !prev)}
          previewSeatIds={selection.previewSeatIds}
          onSeatHover={selection.handleSeatHover}
          onSeatLeave={selection.handleSeatLeave}
          lockedByMe={lockedByMe}
        />
      </div>

      <BookingSummary
        showing={showing}
        selectedSeats={selection.selectedSeats}
        ticketTypes={ticketTypes}
        ticketCounts={selection.ticketCounts}
        onCountChange={selection.handleCountChange}
        email={email}
        onEmailChange={setEmail}
        onConfirm={flow.handleConfirmClick}
        isLoggedIn={!!user}
        loading={flow.submitting}
        maxAvailable={seats.length - unavailableSeatIds.size}
      />

      {flow.showModal && (
        <ConfirmationModal
          showing={showing}
          selectedSeats={selection.selectedSeats}
          email={email}
          onConfirm={flow.handleSubmitBooking}
          onCancel={() => {
            flow.closeModal();
            if (flow.bookingConfirmed) {
              navigate('/');
            }
          }}
          loading={flow.submitting}
        />
      )}
    </div>
  );
}
