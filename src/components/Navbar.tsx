import { Link } from "react-router-dom";
const logo = "/C.png";

export default function Navbar() {
  return (
    <nav className="navbar navbar-expand-lg ch-navbar">
      <div className="container py-2">

        {/* Logo */}
        <Link className="navbar-brand d-flex align-items-center gap-2 text-white" to="/">
          <img src={logo} alt="CinemaHub" width={40} height={40} />
          <span className="fw-semibold">CinemaHub</span>
        </Link>

        {/* Mobile toggle */}
        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#mainNavbar"
        >
          <span className="custom-hamburger"></span>
        </button>


        {/* Right side */}
        <div className="collapse navbar-collapse" id="mainNavbar">
          <ul className="navbar-nav ms-auto align-items-lg-center gap-lg-3 mt-3 mt-lg-0">

            <li className="nav-item">
              <Link className="nav-link" to="/about">
                About
              </Link>
            </li>

            {/* Buttons */}
            <li className="nav-item mt-2 mt-lg-0">
              <Link className="btn ch-btn-outline me-lg-2 w-100 w-lg-auto" to="/signin">
                Sign in
              </Link>
            </li>

            <li className="nav-item mt-2 mt-lg-0">
              <Link className="btn ch-btn-primary w-100 w-lg-auto" to="/register">
                Register
              </Link>
            </li>

          </ul>
        </div>
      </div>
    </nav>
  );
}
