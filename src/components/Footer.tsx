import { Link } from "react-router-dom";

export default function Footer() {
  return (
    <footer className="bg-dark text-light mt-5 border-top">
      <div className="container py-5">

        <div className="row">

          {/* Brand / Description */}
          <div className="col-md-4 mb-4">
            <h5 className="fw-bold">CinemaHub</h5>
            <p className="text-secondary small">
              Upptäck nya filmer, boka dina platser och upplev bio som aldrig förr.
            </p>
          </div>

          {/* Navigation Links */}
          <div className="col-md-4 mb-4">
            <h6 className="fw-semibold">Navigation</h6>

            <ul className="list-unstyled">
              <li>
                <Link className="text-decoration-none text-secondary" to="/about">
                  Om oss
                </Link>
              </li>

              <li>
                <Link className="text-decoration-none text-secondary" to="/contact">
                  Kontakt
                </Link>
              </li>

              <li>
                <Link className="text-decoration-none text-secondary" to="/chat">
                  AI Chat
                </Link>
              </li>
            </ul>
          </div>

          {/* Extra filler section */}
          <div className="col-md-4 mb-4">
            <h6 className="fw-semibold">Support</h6>

            <ul className="list-unstyled text-secondary">
              <li>Integritetspolicy</li>
              <li>Användarvillkor</li>
              <li>Hjälpcenter</li>
            </ul>
          </div>

        </div>

        <hr className="border-secondary" />

        {/* Bottom row */}
        <div className="text-center text-secondary small">
          © {new Date().getFullYear()} CinemaHub. All rights reserved.
        </div>

      </div>
    </footer>
  );
}
