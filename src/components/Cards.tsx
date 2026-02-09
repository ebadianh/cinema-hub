import { useEffect, useState } from "react";

type Film = {
  id: number;
  title: string;
  production_year: number;
  duration_minutes: number;
  genre: string;
  distributor: string;
  language_id: number;
  subtitle_id: number;
  age_rating: number;
  description: string;
};

type Director = {
  id: number;
  film_id: number;
  name: string;
};

type Actor = {
  id: number;
  film_id: number;
  name: string;
  role_order: number;  // För att sortera huvudroller först
};

export default function Cards() {
  const [films, setFilms] = useState<Film[]>([]);
  const [directors, setDirectors] = useState<Director[]>([]);
  const [actors, setActors] = useState<Actor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  function formatDuration(minutes: number) {
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return `${h} tim ${m} min`;
  }



  useEffect(() => {
    const controller = new AbortController();

    (async () => {
      try {
        setLoading(true);
        setError(null);

        const res = await fetch("/api/films", { signal: controller.signal });
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);

        const filmsData = await res.json();
        const filmsList: Film[] = Array.isArray(filmsData) ? filmsData : filmsData.films ?? [];

        const res2 = await fetch("/api/directors", { signal: controller.signal });
        if (!res2.ok) throw new Error(`Directors: ${res2.status} ${res2.statusText}`);
        const directorsData = await res2.json();
        const directorsList: Director[] = Array.isArray(directorsData) ? directorsData : directorsData.directors ?? [];

        const res3 = await fetch("/api/actors", { signal: controller.signal });
        if (!res3.ok) throw new Error(`Actors: ${res3.status} ${res3.statusText}`);
        const actorsData = await res3.json();
        const actorsList: Actor[] = Array.isArray(actorsData) ? actorsData : actorsData.actors ?? [];

        setFilms(filmsList);
        setDirectors(directorsList);
        setActors(actorsList);


      } catch (e: any) {
        if (e.name !== "AbortError") {
          setError(e.message ?? "Failed to load");
        }
      } finally {
        setLoading(false);
      }
    })();

    return () => controller.abort();
  }, []);

  const getDirectorsForFilm = (filmId: number) =>
    directors.filter((d) => d.film_id === filmId);
  const getActorsForFilm = (filmId: number) =>
    actors.filter((a) => a.film_id === filmId)
      .sort((a, b) => (a.role_order ?? 999) - (b.role_order ?? 999));

  if (loading) return <div className="container mt-4">Laddar filmer…</div>;
  if (error) return <div className="container mt-4 text-danger">Error: {error}</div>;

  return (
    <div className="container mt-4">
      <div className="d-flex align-items-end justify-content-between mb-3">
        <h1 className="mb-0">Filmer</h1>
        <span className="text-muted">{films.length} totalt</span>
      </div>

      <div className="row g-3">
        {films.map((f) => (
          <div key={f.id} className="col-12 col-sm-6 col-md-4 col-lg-3">
            <div className="card h-100 shadow-sm">
              <div className="card-body d-flex flex-column">
                <h5 className="card-title mb-1">{f.title}</h5>

                <div className="text-muted small mb-2">
                  {f.production_year} • {formatDuration(f.duration_minutes)} • {f.age_rating}+
                </div>

                <div className="mb-2">
                  <span className="badge text-bg-secondary me-2">{f.genre}</span>
                  <span className="badge text-bg-light">{f.distributor}</span>
                </div>

                <p className="card-text small flex-grow-1">
                  {f.description}
                </p>

                <div className="small">
                  <div>
                    <strong>Regissör:</strong> {" "}
                    {getDirectorsForFilm(f.id).map((d) => d.name).join(", ") || "N/A"}
                  </div>
                  <div className="text-muted">
                    <strong>Skådespelare:</strong> {" "}
                    {getActorsForFilm(f.id).map((a) => a.name).join(", ") || "N/A"}
                  </div>
                </div>

                {/* Optional: debug / extra fields */}
                {/* <div className="text-muted small mt-2">
                  lang_id: {f.language_id} • sub_id: {f.subtitle_id}
                </div> */}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
