import { useEffect, useState } from "react";
import { Link } from "react-router-dom";


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
  role_order: number;  // För att sortera huvudroller först
};

export default function Cards() {
  const [films, setFilms] = useState<Film[]>([]);
  const [directors, setDirectors] = useState<Director[]>([]);
  const [actors, setActors] = useState<Actor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  //filter
  const [selectedAge, setSelectedAge] = useState<string>("all");
  const [selectedGenre, setSelectedGenre] = useState<string>("all");


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

  const filteredFilms = films.filter((film) => {
    if (selectedAge !== "all") {
      const maxAge = parseInt(selectedAge);
      if (film.age_rating > maxAge) {
        return false;
      }
    }

    if (selectedGenre !== "all" && film.genre !== selectedGenre) {
      return false;
    }
    return true;
  });

  return (
    <div className="container mt-4">
      <div className="text-center mb-4">
        <h2 className="section-title d-inline-block position-relative px-4">Filmer</h2>
      </div>

      <div className="d-none d-lg-flex justify-content-between align-items-end mb-4">
        <div className="d-flex gap-3">
          <div style={{ minWidth: '200px' }}>
            <label htmlFor="ageFilter" className="form-label small text-muted">
              Åldersgräns
            </label>
            <select
              id="ageFilter"
              className="form-select"
              value={selectedAge}
              onChange={(e) => setSelectedAge(e.target.value)}>

              <option value="all">Alla åldrar</option>
              <option value="0">Barntillåten</option>
              <option value="7">7+</option>
              <option value="11">11+</option>
              <option value="15">15+</option>
            </select>
          </div>

          <div style={{ minWidth: '200px' }}>
            <label htmlFor="ageGenreFilter" className="form-label small text-muted">
              Genre
            </label>
            <select
              id="ageGenreFilter"
              className="form-select"
              value={selectedGenre}
              onChange={(e) => setSelectedGenre(e.target.value)}>

              <option value="all">Alla genrer</option>
              <option value="Drama">Drama</option>
              <option value="Action">Action</option>
              <option value="Komedi">Komedi</option>
              <option value="Sci-Fi">Sci-Fi</option>
              <option value="Animerat">Animerat</option>
              <option value="Thriller">Thriller</option>
              <option value="Skräck">Skräck</option>
            </select>
          </div>
        </div>

        <div className="text-muted small pb-2">
          {filteredFilms.length} av {films.length} filmer
        </div>
      </div>

      <div className="d-lg-none mb-4">
        <div className="row g-3">
          <div className="col-6">
            <label htmlFor="ageFilterMobile" className="form-label small text-muted">Åldersgräns</label>
            <select
              id="ageFilterMobile"
              className="form-select"
              value={selectedAge}
              onChange={(e) => setSelectedAge(e.target.value)}>

              <option value="all">Alla åldrar</option>
              <option value="0">Barntillåten</option>
              <option value="7">7+</option>
              <option value="11">11+</option>
              <option value="15">15+</option>
            </select>
          </div>
          <div className="col-6">
            <label htmlFor="genreFilterMobile" className="form-label small text-muted">Genre</label>
            <select
              id="genreFilterMobile"
              className="form-select"
              value={selectedGenre}
              onChange={(e) => setSelectedGenre(e.target.value)}>

              <option value="all">Alla genrer</option>
              <option value="Drama">Drama</option>
              <option value="Action">Action</option>
              <option value="Komedi">Komedi</option>
              <option value="Sci-Fi">Sci-Fi</option>
              <option value="Animerat">Animerat</option>
              <option value="Thriller">Thriller</option>
              <option value="Skräck">Skräck</option>
            </select>
          </div>
        </div>
      </div>

      <div className="row g-3">
        {filteredFilms.map((f) => (
          <div key={f.id} className="col-6 col-md-4 col-lg-2">
            <div className="card h-100 shadow-sm p-0 overflow-hidden">
              <img src={f.images && f.images.length > 0 ? f.images[0] : '/placeholder.jpg'}
                className="card-img-top w-100"
                alt={f.title}
                style={{ height: "350px", objectFit: "cover" }} />
              <div className="card-body d-flex flex-column p-2">
                <h5 className="card-title small mb-1 text-truncate">{f.title}</h5>

                <div className="mb-2">
                  <span className="badge text-bg-secondary me-2">{f.genre}</span>
                  <span className="badge text-bg-light">{f.distributor}</span>
                </div>

                <Link className="btn btn-primary mt-3" to={`/films/${f.id}`}>
                  Mer info
                </Link>




                {/* Optional: debug / extra fields */}
                {/* <div className="text-muted small mt-2">
                  lang_id: {f.language_id} • sub_id: {f.subtitle_id}
                </div> */}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div >
  );
}
