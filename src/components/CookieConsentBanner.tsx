import { useCookieConsent } from "../context/CookieConsentContext";

export default function CookieConsentBanner() {
  const { status, accept, reject } = useCookieConsent();

  if (status !== "unknown") {
    return null;
  }

  return (
    <div
      className="position-fixed bottom-0 start-0 end-0 bg-dark text-white py-3 px-3 px-md-4"
      style={{ zIndex: 1050 }}
    >
      <div className="container d-flex flex-column flex-md-row align-items-md-center justify-content-between gap-2">
        <p className="mb-2 mb-md-0">
          We use cookies to improve your experience. You can accept or decline non-essential cookies.
        </p>
        <div className="d-flex gap-2 justify-content-end">
          <button
            type="button"
            className="btn btn-outline-light btn-sm"
            onClick={reject}
          >
            Decline
          </button>
          <button
            type="button"
            className="btn btn-primary btn-sm"
            onClick={accept}
          >
            Accept
          </button>
        </div>
      </div>
    </div>
  );
}

