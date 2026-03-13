import { Link } from "react-router-dom";
import type User from "../interfaces/Users";
import { useState, useEffect } from "react";

const logo = "/C.png";

interface NavbarProps {
  user: User | null;
  setUser: (user: User | null) => void;
}

export default function Navbar({ user, setUser }: NavbarProps) {
  const [confirmingLogout, setConfirmingLogout] = useState(false);
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 50);
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

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

  const handleLogoutClick = () => {
    if (confirmingLogout) {
      setConfirmingLogout(false);
      logout();
    } else {
      setConfirmingLogout(true);
    }
  };

  return (
    <nav className={`navbar navbar-expand-lg ch-navbar ${scrolled ? 'ch-navbar-scrolled' : ''}`}>
      <div className="container py-2">
        <Link className="navbar-brand d-flex align-items-center gap-2 text-white" to="/">
          <img src={logo} alt="CinemaHub" width={40} height={40} />
          <span className="fw-semibold">CinemaHub</span>
        </Link>

        <button
          className="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#mainNavbar"
          aria-controls="mainNavbar"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span className="custom-hamburger"></span>
        </button>

        <div className="collapse navbar-collapse" id="mainNavbar">
          <ul className="navbar-nav ms-auto align-items-lg-center gap-lg-3 mt-3 mt-lg-0">
            <li className="nav-item">
              <Link className="nav-link" to="/about">
                Om oss
              </Link>
            </li>

            {user ? (
              <>
                {user.role === 'admin' && (
                  <li className="nav-item">
                    <Link className="nav-link" to="/admin">
                      Admin
                    </Link>
                  </li>
                )}
                <li className="nav-item mt-2 mt-lg-0">
                  <Link className="btn ch-btn-outline" to="/profile">
                    Profil ({user.firstName})
                  </Link>
                </li>
                <li className="nav-item mt-2 mt-lg-0">
                  <button
                    className={`btn w-100 w-lg-auto ch-btn-logout ${
                      confirmingLogout ? 'ch-btn-danger' : 'ch-btn-primary'
                    }`}
                    onClick={handleLogoutClick}
                  >
                    {confirmingLogout ? 'Bekräfta' : 'Logga ut'}
                  </button>
                </li>
              </>
            ) : (
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