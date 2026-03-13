import { useEffect, useState } from "react";

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

  if (loading) return <div>Laddar filmer...</div>;
  if (error) return <div>{error}</div>;

  return (
    <div className="container mt-4">
      <h1>Adminpanel – Filmer</h1>

      <div className="table-responsive mt-4">
        <table className="table table-bordered table-striped align-middle">
          <thead>
            <tr>
              <th>ID</th>
              <th>Titel</th>
              <th>Genre</th>
              <th>Åldersgräns</th>
              <th>Längd (min)</th>
            </tr>
          </thead>
          <tbody>
            {films.map((film) => (
              <tr key={film.id}>
                <td>{film.id}</td>
                <td>{film.title}</td>
                <td>{film.genre}</td>
                <td>{film.age_rating}</td>
                <td>{film.duration_minutes}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
