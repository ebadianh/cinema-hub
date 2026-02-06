import { Link } from "react-router-dom";
const logo = "/C.png";

export default function Navbar() {
  return (
    <nav className="navbar navbar-expand-lg bg-white border-bottom">
      <div className="container py-2">
        {/* Logo left */}
        <Link className="navbar-brand d-flex align-items-center gap-2" to="/">
          <img
            src={logo}
            alt="CinemaHub"
            width={40}
            height={40}
            style={{ objectFit: "contain" }}
          />
          <span className="fw-semibold">CinemaHub</span>
        </Link>

        {/* Toggler for mobile */}
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#mainNavbar"
          aria-controls="mainNavbar"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon" />
        </button>

        {/* Right side */}
        <div className="collapse navbar-collapse" id="mainNavbar">
          <ul className="navbar-nav ms-auto align-items-lg-center gap-lg-2 mt-3 mt-lg-0">
            <li className="nav-item">
              <Link className="nav-link" to="/about">
                About
              </Link>
            </li>

            <li className="nav-item">
              <Link className="nav-link" to="/contact">
                Contact
              </Link>
            </li>

            <li className="nav-item">
              <Link className="nav-link" to="/ai-chat">
                AI Chat
              </Link>
            </li>

            {/* Buttons */}
            <li className="nav-item mt-2 mt-lg-0">
              <Link className="btn btn-outline-dark me-lg-2 w-100 w-lg-auto" to="/signin">
                Sign in
              </Link>
            </li>

            <li className="nav-item mt-2 mt-lg-0">
              <Link className="btn btn-dark w-100 w-lg-auto" to="/register">
                Register
              </Link>
            </li>
          </ul>
        </div>
      </div>
    </nav>
  );
}
