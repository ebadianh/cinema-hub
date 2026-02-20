import { useEffect, useState } from "react";
import { Link, useParams, useNavigate } from "react-router-dom";

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

  // I din DB verkar dessa vara JSON-strängar ibland
  images: string[] | string;
  trailers?: string[] | string;
};

type Director = { id: number; film_id: number; name: string };
type Actor = { id: number; film_id: number; name: string; role_order: number };
type Showing = { id: number; film_id: number; salong_id: number; start_time: string; language: string; subtitle: string; salong_name?: string };

function parseJsonArray(value: unknown): string[] {
  if (!value) return [];
  if (Array.isArray(value)) return value.filter(Boolean) as string[];

  if (typeof value === "string") {
    const s = value.trim();

    // ifall backend redan skickar "http..." utan JSON
    if (s.startsWith("http")) return [s];

    try {
      const parsed = JSON.parse(s);
      return Array.isArray(parsed) ? parsed.filter(Boolean) : [];
    } catch {
      return [];
    }
  }

  return [];
}

function toYouTubeEmbed(url: string): string {
  try {
    const u = new URL(url);

    // youtu.be/ID
    if (u.hostname.includes("youtu.be")) {
      const id = u.pathname.replace("/", "");
      return `https://www.youtube.com/embed/${id}`;
    }

    // youtube.com/watch?v=ID
    const v = u.searchParams.get("v");
    if (v) return `https://www.youtube.com/embed/${v}`;

    // already embed
    if (u.pathname.includes("/embed/")) return url;

    return url;
  } catch {
    return url;
  }
}

export default function FilmDetails() {
  const { id } = useParams();
  const filmId = Number(id);

  const [film, setFilm] = useState<Film | null>(null);
  const [directors, setDirectors] = useState<Director[]>([]);
  const [actors, setActors] = useState<Actor[]>([]);
  const [showings, setShowings] = useState<Showing[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  const pageStyle: React.CSSProperties = {
    minHeight: "100vh",
    padding: "28px 0 60px",
    color: "rgba(255,255,255,.92)",
    background:
      "radial-gradient(1000px 500px at 20% 15%, rgba(90,120,200,.20), transparent 60%)," +
      "radial-gradient(900px 500px at 70% 40%, rgba(60,90,170,.16), transparent 60%)," +
      "linear-gradient(180deg, #050a14, #070e1b 45%, #050a14)",
  };

  const glassCardStyle: React.CSSProperties = {
    background: "rgba(255,255,255,.04)",
    border: "1px solid rgba(255,255,255,.07)",
    boxShadow: "0 18px 50px rgba(0,0,0,.35), 0 2px 0 rgba(255,255,255,.04) inset",
    backdropFilter: "blur(10px)",
  };

  function formatDuration(minutes: number) {
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return `${h}h ${m}m`;
  }

  useEffect(() => {
    const controller = new AbortController();

    (async () => {
      try {
        setLoading(true);
        setError(null);

        // 1) FILM by id
        const res = await fetch(`/api/films/${filmId}`, { signal: controller.signal });
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        const filmData = await res.json();
        setFilm(filmData ?? null);

        // 2) Directors + Actors (hämtas som listor och filtreras)
        const [res2, res3] = await Promise.all([
          fetch("/api/directors", { signal: controller.signal }),
          fetch("/api/actors", { signal: controller.signal }),
        ]);

        if (!res2.ok) throw new Error(`Directors: ${res2.status} ${res2.statusText}`);
        if (!res3.ok) throw new Error(`Actors: ${res3.status} ${res3.statusText}`);

        const directorsData = await res2.json();
        const actorsData = await res3.json();

        const directorsList: Director[] = Array.isArray(directorsData)
          ? directorsData
          : directorsData.directors ?? [];

        const actorsList: Actor[] = Array.isArray(actorsData)
          ? actorsData
          : actorsData.actors ?? [];

        setDirectors(directorsList.filter(d => d.film_id === filmId));
        setActors(
          actorsList
            .filter(a => a.film_id === filmId)
            .sort((a, b) => (a.role_order ?? 999) - (b.role_order ?? 999))
        );

        // Hämta visningar för denna film
        const resShowings = await fetch(`/api/showings?where=film_id=${filmId}`, { signal: controller.signal });
        if (resShowings.ok) {
          const showingsData = await resShowings.json();
          const showingsList: Showing[] = Array.isArray(showingsData) ? showingsData : showingsData.showings ?? [];
          setShowings(showingsList);
        }
      } catch (e: any) {
        if (e.name !== "AbortError") setError(e.message ?? "Failed to load");
      } finally {
        setLoading(false);
      }
    })();

    return () => controller.abort();
  }, [filmId]);

  if (loading) return <div className="container mt-4">Laddar film…</div>;
  if (error) return <div className="container mt-4 text-danger">Error: {error}</div>;
  if (!film) return <div className="container mt-4">Ingen film hittades.</div>;

  // --- Parse images + trailers from DB ---
  const images = parseJsonArray(film.images);
  const poster = images[0] ?? "/placeholder.jpg";

  const trailers = parseJsonArray(film.trailers);
  const trailerUrl = trailers[0];
  const trailerEmbedUrl = trailerUrl ? toYouTubeEmbed(trailerUrl) : null;

  const directorNames = directors.map(d => d.name).join(", ") || "N/A";
  const actorNames = actors.map(a => a.name).join(", ") || "N/A";

  return (
    <div style={pageStyle}>
      <div className="container" style={{ maxWidth: 1200 }}>
        <div className="mb-3">
          <Link
            to="/"
            className="text-decoration-none fw-semibold"
            style={{ color: "rgba(255,255,255,.75)" }}
          >
            ← Back
          </Link>
        </div>

        <div className="row g-4 align-items-start">
          {/* Poster */}
          <div className="col-12 col-lg-4">
            <div className="rounded-4 overflow-hidden" style={glassCardStyle}>
              <img src={poster} alt={film.title} className="w-100 d-block" />
            </div>
          </div>

          {/* Info */}
          <div className="col-12 col-lg-8">
            <div className="rounded-4 p-4" style={glassCardStyle}>
              <h1 className="mb-2" style={{ fontSize: "2.2rem", letterSpacing: ".2px" }}>
                {film.title}
              </h1>

              <div
                className="d-flex flex-wrap align-items-center gap-2 fw-semibold"
                style={{ color: "rgba(255,255,255,.70)" }}
              >
                <span>{film.production_year}</span>
                <span style={{ opacity: 0.55 }}>•</span>
                <span>{film.genre}</span>
                <span style={{ opacity: 0.55 }}>•</span>
                <span>{formatDuration(film.duration_minutes)}</span>
              </div>

              <hr className="my-3" style={{ borderColor: "rgba(255,255,255,.08)" }} />

              <p
                className="mb-0"
                style={{
                  lineHeight: 1.6,
                  color: "rgba(255,255,255,.78)",
                  maxWidth: "72ch",
                }}
              >
                {film.description}
              </p>

              <hr className="my-3" style={{ borderColor: "rgba(255,255,255,.08)" }} />

              <div className="d-grid" style={{ rowGap: 10 }}>
                <div className="d-grid" style={{ gridTemplateColumns: "92px 1fr", columnGap: 10 }}>
                  <div className="fw-bold" style={{ color: "rgba(255,255,255,.70)" }}>Director:</div>
                  <div className="fw-semibold" style={{ color: "rgba(255,255,255,.88)" }}>{directorNames}</div>
                </div>

                <div className="d-grid" style={{ gridTemplateColumns: "92px 1fr", columnGap: 10 }}>
                  <div className="fw-bold" style={{ color: "rgba(255,255,255,.70)" }}>Cast:</div>
                  <div className="fw-semibold" style={{ color: "rgba(255,255,255,.88)" }}>{actorNames}</div>
                </div>

                <div className="d-grid" style={{ gridTemplateColumns: "92px 1fr", columnGap: 10 }}>
                  <div className="fw-bold" style={{ color: "rgba(255,255,255,.70)" }}>Rating:</div>
                  <div className="fw-semibold" style={{ color: "rgba(255,255,255,.88)" }}>{film.age_rating}+</div>
                </div>

                {showings.length > 0 && (
                  <div className="d-grid" style={{ gridTemplateColumns: "92px 1fr", columnGap: 10 }}>
                    <div className="fw-bold" style={{ color: "rgba(255,255,255,.70)" }}>Visningar:</div>
                    <div className="d-flex flex-wrap gap-2">
                      {showings.map(s => (
                        <button
                          key={s.id}
                          onClick={() => navigate(`/booking/${s.id}`)}
                          className="px-3 py-2 rounded-3 fw-bold btn btn-outline-light"
                          style={{
                            fontSize: ".9rem",
                            color: "rgba(255,255,255,.88)",
                            background: "rgba(255,255,255,.06)",
                            border: "1px solid rgba(255,255,255,.08)",
                          }}
                        >
                          {new Date(s.start_time).toLocaleDateString("sv-SE", { weekday: "short", day: "numeric", month: "short" })}{" "}
                          {new Date(s.start_time).toLocaleTimeString("sv-SE", { hour: "2-digit", minute: "2-digit" })}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {showings.length > 0 && (
                <div className="d-flex flex-wrap gap-3 mt-4">
                  <button
                    className="btn btn-danger fw-bold px-4 py-2 rounded-3"
                    onClick={() => navigate(`/booking/${showings[0].id}`)}
                  >
                    Boka biljetter
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Trailer */}
        <div className="mt-4">
          <h2 className="mb-3 fw-bold" style={{ fontSize: "1.3rem", color: "rgba(255,255,255,.92)" }}>
            Watch Trailer
          </h2>

          <div className="rounded-4 overflow-hidden" style={glassCardStyle}>
            {trailerEmbedUrl ? (
              <div className="ratio ratio-21x9">
                <iframe
                  src={trailerEmbedUrl}
                  title={`${film.title} Trailer`}
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                  allowFullScreen
                />
              </div>
            ) : (
              <div className="p-4" style={{ color: "rgba(255,255,255,.75)" }}>
                Ingen trailer tillgänglig.
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
