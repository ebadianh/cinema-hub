import { useEffect, useState } from "react";
import FilmModal from "../components/Admin/FilmModal";

// Film DTO för admins att kunna göra fetch på alla våra filmer
type Film = {
  id: number;
  title: string;
  description: string;
  duration_minutes: number;
  age_rating: string;
  genre: string;
  images: string[];
  trailers: string[];
};

export default function AdminFilms() {
  const [films, setFilms] = useState<Film[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [editFilm, setEditFilm] = useState<Film | null>(null);

  // försök hämta alla filmer från databas till denna URL'n
  useEffect(() => {
    fetch("/api/admin/films", {
      credentials: "include",
    })
      .then(async (res) => {
        const data = await res.json();

        if (!res.ok) {
          throw new Error(data?.error || "Kunde inte hämta filmer.");
        }
        // om allt ok, returnera filmer
        return data;
      })
      .then((data) => {
        setFilms(data);
      })
      .catch((err) => {
        setError(err.message || "Ett fel uppstod.");
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  const handleDelete = async (filmId: number, filmTitle: string) => {
    // Bekräfta innan borttagning
    if (!confirm(`Är du säker på att du vill ta bort "${filmTitle}"?`)) {
      return;
    }

    try {
      const res = await fetch(`/api/admin/films/${filmId}`, {
        method: "DELETE",
        credentials: "include",
      });

      const data = await res.json();

      if (!res.ok || data.error) {
        alert(data?.error || "Kunde inte ta bort filmen.");
        return;
      }

      // Ta bort från state
      setFilms((prev) => prev.filter((f) => f.id !== filmId));
      alert(`"${filmTitle}" har tagits bort.`);
    } catch (err) {
      alert("Ett fel uppstod vid borttagning.");
    }
  };

  const refreshFilms = () => {
    fetch("/api/admin/films", {
      credentials: "include",
    })
      .then((res) => res.json())
      .then((data) => setFilms(data))
      .catch((err) => console.error(err));
  };

  const filteredFilms = films.filter((film) => {
    const search = searchQuery.toLowerCase();
    return (
      film.title.toLowerCase().includes(search) ||
      film.genre.toLowerCase().includes(search) ||
      film.age_rating.toString().includes(search)
    );
  });

  if (loading) return <div>Laddar filmer...</div>;
  if (error) return <div>{error}</div>;

  return (
    <div className="container-fluid py-4">
      {/* Header */}
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="mb-0"
          style={{ color: 'var(--ch-text)', fontSize: 'clamp(1,5rem, 4vw, 2rem)' }}>
          Adminpanel – Filmer
        </h1>
        <button
          className="btn ch-btn-primary"
          onClick={() => setShowModal(true)}>
          <span className="d-none d-md-inline">+ Lägg till film</span>
          <span className="d-md-none">+</span>
        </button>
      </div>

      <div className="mb-4">
        <input
          type="text"
          className="form-control form-control-lg bg-dark text-light"
          placeholder="🔍 Sök filmer (titel, genre, åldersgräns...)"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          style={{
            border: "1px solid var(--ch-border)",
            borderRadius: "8px"
          }}
        />
      </div>

      {/* Desktop/Tablet: Tabell */}
      <div className="d-none d-md-block">
        <div className="card bg-dark text-light border-0 shadow">
          <div className="card-body p-0">
            <div className="table-responsive">
              <table className="table table-dark table-hover mb-0">
                <thead>
                  <tr style={{ borderBottom: "2px solid var(--ch-border)" }}>
                    <th style={{ padding: "1rem" }}>ID</th>
                    <th>Titel</th>
                    <th>Genre</th>
                    <th>Åldersgrän</th>
                    <th>Längd</th>
                    <th className="text-end">Åtgärder</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredFilms.map((film) => (
                    <tr key={film.id}>
                      <td style={{ padding: "1rem" }}>{film.id}</td>
                      <td>{film.title}</td>
                      <td>
                        <span className="badge bg-secondary">{film.genre}</span>
                      </td>
                      <td>{film.age_rating}</td>
                      <td>{film.duration_minutes} min</td>
                      <td className="text-end">
                        <button
                          className="btn btn-sm ch-btn-outline me-2"
                          onClick={() => setEditFilm(film)}>
                          Redigera
                        </button>
                        <button
                          className="btn btn-sm ch-btn-danger"
                          onClick={() => handleDelete(film.id, film.title)}>
                          Ta bort
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>


      {/* Mobile */}
      <div className="d-md-none">
        <div className="row g-3">
          {filteredFilms.map((film) => (
            <div key={film.id} className="col-12">
              <div className="card bg-dark text-light border-0 shadow">
                <div className="card-body">
                  <div className="d-flex justify-content-between align-items-start mb-2">
                    <h5 className="mb-0" style={{ color: 'var(--ch-text)' }}>
                      {film.title}
                    </h5>
                    <span className="badge bg-secondary">{film.genre}</span>
                  </div>

                  <div className="small text-muted mb-3">
                    <span className="me-3">ID: {film.id}</span>
                    <span className="me-3">Ålder: {film.age_rating}</span>
                    <span>{film.duration_minutes} min</span>
                  </div>

                  <div className="d-flex gap-2">
                    <button
                      className="btn btn-sm ch-btn-outline flex-grow-1"
                      onClick={() => setEditFilm(film)}
                    >
                      Redigera
                    </button>
                    <button
                      className="btn btn-sm ch-btn-danger flex-grow-1"
                      onClick={() => handleDelete(film.id, film.title)}
                    >
                      Ta bort
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <FilmModal
        isOpen={showModal || editFilm !== null}
        onClose={() => {
          setShowModal(false);
          setEditFilm(null);
        }}
        onSave={() => {
          refreshFilms();
          setEditFilm(null);
        }}
        film={editFilm}
      />
    </div>
  );
}