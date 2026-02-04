import { useEffect, useState } from "react";

type Film = {
  id: number;
  title: string;
  production_year: number;
  length_minutes: number;
  genre: string;
  distributor: string;
  language_id: number;
  subtitle_id: number;
  age_limit: number;
  description: string;
  director: string;
  actors: string;
};

export default function Cards() {
  const [films, setFilms] = useState<Film[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    (async () => {
      try {
        setLoading(true);
        setError(null);

        const res = await fetch("/api/films", { signal: controller.signal });
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);

        const data = await res.json();
        const list: Film[] = Array.isArray(data) ? data : data.films ?? [];

        setFilms(list);
      } catch (e: any) {
        if (e.name !== "AbortError") {
          setError(e.message ?? "Failed to load films");
        }
      } finally {
        setLoading(false);
      }
    })();

    return () => controller.abort();
  }, []);

  if (loading) return <div className="container mt-4">Loading films…</div>;
  if (error) return <div className="container mt-4 text-danger">Error: {error}</div>;

  return (
    <div className="container mt-4">
      <div className="d-flex align-items-end justify-content-between mb-3">
        <h1 className="mb-0">Films</h1>
        <span className="text-muted">{films.length} total</span>
      </div>

      <div className="row g-3">
        {films.map((f) => (
          <div key={f.id} className="col-12 col-sm-6 col-md-4 col-lg-3">
            <div className="card h-100 shadow-sm">
              <div className="card-body d-flex flex-column">
                <h5 className="card-title mb-1">{f.title}</h5>

                <div className="text-muted small mb-2">
                  {f.production_year} • {f.length_minutes} min • {f.age_limit}+
                </div>

                <div className="mb-2">
                  <span className="badge text-bg-secondary me-2">{f.genre}</span>
                  <span className="badge text-bg-light">{f.distributor}</span>
                </div>

                <p className="card-text small flex-grow-1">
                  {f.description}
                </p>

                <div className="small">
                  <div><strong>Director:</strong> {f.director}</div>
                  <div className="text-muted"><strong>Actors:</strong> {f.actors}</div>
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
