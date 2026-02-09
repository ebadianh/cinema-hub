import { Link } from "react-router-dom";
import type User from "../interfaces/Users";
const logo = "/C.png";

  interface NavbarProps {
    user: User | null;
    setUser: (user: User | null) => void;
  }

export default function Navbar({ user, setUser }: NavbarProps) {
  const logout = async () => {
    try {
      await fetch('/api/login', {
        method: 'DELETE',
        credentials: 'include'
      });
      setUser(null);
    } catch (error) {
      console.error("Logout misslyckades:", error);
    }
  };

  
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
                Om oss
              </Link>
            </li>
            {user ? (
              // INLOGGAD:
              <>
              <li className="nav-item mt-2 mt-lg-0">
                <Link className="btn ch-btn-outline" to="/profile">
                  Profil ({user.firstName})
                </Link>
              </li>
              <li className="nav-item mt-2 mt-lg-0">
                <button className="btn ch-btn-primary w-100 w-lg-auto" onClick={logout}>
                  Logga ut
                </button>
              </li>
              </>
            ) : (
                // INTE INLOGGAD:
              <>
                <li className="nav-item mt-2 mt-lg-0">
                  <Link className="btn ch-btn-outline" to="/login">
                    Logga in
                  </Link>
                </li>
                <li className="nav-item mt-2 mt-lg-0">
                  <Link className="btn ch-btn-primary" to="/register">
                    Registrera
                  </Link>
                </li>
              </>
            )}


          </ul>
        </div>
      </div>
    </nav>
  );
}
