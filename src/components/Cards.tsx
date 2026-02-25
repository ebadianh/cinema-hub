import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import Filter from "./Filter.tsx";

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
  images: string[];
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
  role_order: number;
};

export default function Cards() {
  const [films, setFilms] = useState<Film[]>([]);
  const [directors, setDirectors] = useState<Director[]>([]);
  const [actors, setActors] = useState<Actor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedAge, setSelectedAge] = useState<string>("all");
  const [selectedGenre, setSelectedGenre] = useState<string>("all");

  useEffect(() => {
    const controller = new AbortController();

    (async () => {
      try {
        setLoading(true);
        setError(null);

        const res = await fetch("/api/films", { signal: controller.signal });
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);

        const filmsData = await res.json();
        const filmsList: Film[] =
          Array.isArray(filmsData) ? filmsData : filmsData.films ?? [];

        const res2 = await fetch("/api/directors", { signal: controller.signal });
        if (!res2.ok)
          throw new Error(`Directors: ${res2.status} ${res2.statusText}`);
        const directorsData = await res2.json();
        const directorsList: Director[] =
          Array.isArray(directorsData)
            ? directorsData
            : directorsData.directors ?? [];

        const res3 = await fetch("/api/actors", { signal: controller.signal });
        if (!res3.ok)
          throw new Error(`Actors: ${res3.status} ${res3.statusText}`);
        const actorsData = await res3.json();
        const actorsList: Actor[] =
          Array.isArray(actorsData) ? actorsData : actorsData.actors ?? [];

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

  if (loading)
    return <div className="container mt-4">Laddar filmer…</div>;

  if (error)
    return (
      <div className="container mt-4 text-danger">Error: {error}</div>
    );

  const filteredFilms = films.filter((film) => {
    if (selectedAge !== "all") {
      const maxAge = parseInt(selectedAge);
      if (film.age_rating > maxAge) return false;
    }

    if (selectedGenre !== "all" && film.genre !== selectedGenre)
      return false;

    return true;
  });

  return (
    <div className="container mt-4">

      <div className="text-center mb-4">
        <h2 className="section-title d-inline-block position-relative px-4">
          Filmer
        </h2>
      </div>

      <Filter
        selectedAge={selectedAge}
        selectedGenre={selectedGenre}
        onAgeChange={setSelectedAge}
        onGenreChange={setSelectedGenre}
        filteredCount={filteredFilms.length}
        totalCount={films.length}
      />

      <div className="row g-3">

        {filteredFilms.map((f) => (

          <div key={f.id} className="col-6 col-md-4 col-lg-2">

            {/* HELA KORTET ÄR NU KLICKBART */}
            <Link
              to={`/films/${f.id}`}
              className="text-decoration-none text-dark"
            >

              <div className="card h-100 shadow-sm p-0 overflow-hidden card-hover">

                {/* Poster */}
                <div className="poster-wrapper">
                  <img
                    src={
                      f.images && f.images.length > 0
                        ? f.images[0]
                        : "/placeholder.jpg"
                    }
                    className="poster-img"
                    alt={f.title}
                  />
                </div>

                {/* Info */}
                <div className="card-body text-center p-2">

                  <h5 className="card-title small mb-1">
                    {f.title}
                  </h5>

                  <div className="mb-2">
                    <span className="badge text-bg-secondary me-1">
                      {f.genre}
                    </span>

                    <span className="badge text-bg-light">
                      {f.distributor}
                    </span>
                  </div>


                </div>

              </div>

            </Link>

          </div>

        ))}

      </div>

    </div>
  );
}