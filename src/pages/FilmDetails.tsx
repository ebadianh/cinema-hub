import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
 
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
 
  images: string[] | string;
  trailers?: string[] | string;
};
 
type Director = { id: number; film_id: number; name: string };
type Actor = { id: number; film_id: number; name: string; role_order: number };
 
function parseJsonArray(value: unknown): string[] {
  if (!value) return [];
  if (Array.isArray(value)) return value.filter(Boolean) as string[];
 
  if (typeof value === "string") {
    const s = value.trim();
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
 
    if (u.hostname.includes("youtu.be")) {
      const id = u.pathname.replace("/", "");
      return `https://www.youtube.com/embed/${id}`;
    }
 
    const v = u.searchParams.get("v");
    if (v) return `https://www.youtube.com/embed/${v}`;
 
    if (u.pathname.includes("/embed/")) return url;
 
    return url;
  } catch {
    return url;
  }
}
 
function withAutoplay(embedUrl: string): string {
  try {
    const u = new URL(embedUrl);
    u.searchParams.set("autoplay", "1");
    u.searchParams.set("mute", "1"); // autoplay funkar oftare
    u.searchParams.set("rel", "0");
    return u.toString();
  } catch {
    const joiner = embedUrl.includes("?") ? "&" : "?";
    return `${embedUrl}${joiner}autoplay=1&mute=1&rel=0`;
  }
}
 
function getYouTubeId(url: string): string | null {
  try {
    const u = new URL(url);
 
    if (u.hostname.includes("youtu.be")) {
      const id = u.pathname.replace("/", "");
      return id || null;
    }
 
    const v = u.searchParams.get("v");
    if (v) return v;
 
    const parts = u.pathname.split("/").filter(Boolean);
    const embedIndex = parts.indexOf("embed");
    if (embedIndex !== -1 && parts[embedIndex + 1]) return parts[embedIndex + 1];
 
    return null;
  } catch {
    return null;
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
 
  const [isTrailerOpen, setIsTrailerOpen] = useState(false);
  const [useThumbFallback, setUseThumbFallback] = useState(false);
 
  // Mock showtimes tills du kopplar showings från DB
  const showtimes = useMemo(() => ["16:30", "19:15", "21:45"], []);
 
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
 
        const res = await fetch(`/api/films/${filmId}`, { signal: controller.signal });
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
        const filmData = await res.json();
        setFilm(filmData ?? null);
 
        const [res2, res3] = await Promise.all([
          fetch("/api/directors", { signal: controller.signal }),
          fetch("/api/actors", { signal: controller.signal }),
        ]);
 
        if (!res2.ok) throw new Error(`Regissörer: ${res2.status} ${res2.statusText}`);
        if (!res3.ok) throw new Error(`Skådespelare: ${res3.status} ${res3.statusText}`);
 
        const directorsData = await res2.json();
        const actorsData = await res3.json();
 
        const directorsList: Director[] = Array.isArray(directorsData)
          ? directorsData
          : directorsData.directors ?? [];
 
        const actorsList: Actor[] = Array.isArray(actorsData)
          ? actorsData
          : actorsData.actors ?? [];
 
        setDirectors(directorsList.filter((d) => d.film_id === filmId));
        setActors(
          actorsList
            .filter((a) => a.film_id === filmId)
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
        if (e.name !== "AbortError") setError(e.message ?? "Kunde inte ladda");
      } finally {
        setLoading(false);
      }
    })();
 
    return () => controller.abort();
  }, [filmId]);
 
  // ESC stänger + lås scroll när modal är öppen
  useEffect(() => {
    if (!isTrailerOpen) return;
 
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") setIsTrailerOpen(false);
    };
 
    window.addEventListener("keydown", onKeyDown);
 
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
 
    return () => {
      window.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = prev;
    };
  }, [isTrailerOpen]);
 
  if (loading) return <div className="container mt-4">Laddar film…</div>;
  if (error) return <div className="container mt-4 text-danger">Fel: {error}</div>;
  if (!film) return <div className="container mt-4">Ingen film hittades.</div>;
 
  const images = parseJsonArray(film.images);
  const poster = images[0] ?? "/placeholder.jpg";
 
  const trailers = parseJsonArray(film.trailers);
  const trailerUrl = trailers[0];
  const trailerEmbedUrl = trailerUrl ? toYouTubeEmbed(trailerUrl) : null;
  const trailerAutoplayUrl = trailerEmbedUrl ? withAutoplay(trailerEmbedUrl) : null;
 
  const directorNames = directors.map((d) => d.name).join(", ") || "N/A";
  const actorNames = actors.map((a) => a.name).join(", ") || "N/A";
 
  // Thumbnail URL (maxres -> fallback hq)
  const ytId = trailerUrl ? getYouTubeId(trailerUrl) : null;
  const thumbPrimary = ytId ? `https://i.ytimg.com/vi/${ytId}/maxresdefault.jpg` : null;
  const thumbFallback = ytId ? `https://i.ytimg.com/vi/${ytId}/hqdefault.jpg` : null;
  const thumbSrc = useThumbFallback ? thumbFallback : thumbPrimary;
 
  return (
    <div className="container py-4">
      <div className="mb-3">
        <Link to="/" className="text-decoration-none fw-semibold text-muted">
          ← Tillbaka
        </Link>
      </div>
 
      {/* ✅ Mobil (xs/sm): trailer överst + poster-kort överlappande till höger */}
      <div className="d-block d-md-none">
        <section className="mb-4">
          <h2 className="h5 mb-3 text-center text-md-start">Se trailer</h2>
 
          <div className="position-relative" style={{ maxWidth: 900, margin: "0 auto" }}>
            {/* Thumbnail + overlay (robust, ingen button+ratio) */}
            <div
              className="ratio ratio-16x9 overflow-hidden"
              style={{
                borderRadius: 12,
                background: "#0b0f17",
                boxShadow: "0 10px 30px rgba(0,0,0,.25)",
              }}
            >
              {trailerEmbedUrl && thumbSrc ? (
                <img
                  src={thumbSrc}
                  alt=""
                  className="w-100 h-100 d-block"
                  style={{ objectFit: "cover" }}
                  onError={() => {
                    // om maxres saknas -> fallback
                    if (!useThumbFallback) setUseThumbFallback(true);
                  }}
                />
              ) : (
                <div className="d-flex align-items-center justify-content-center w-100 h-100">
                  <span className="text-muted">Ingen trailer tillgänglig.</span>
                </div>
              )}
 
              {/* Gradient overlay */}
              <div
                style={{
                  position: "absolute",
                  inset: 0,
                  background:
                    "linear-gradient(180deg, rgba(0,0,0,.10), rgba(0,0,0,.45) 70%, rgba(0,0,0,.65))",
                }}
              />
 
              {/* Click overlay + play */}
              {trailerEmbedUrl && (
                <div
                  role="button"
                  tabIndex={0}
                  onClick={() => setIsTrailerOpen(true)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === " ") setIsTrailerOpen(true);
                  }}
                  style={{
                    position: "absolute",
                    inset: 0,
                    cursor: "pointer",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                  }}
                  aria-label="Spela upp trailer"
                >
             <div
  style={{
    width: 64,
    height: 64,
    borderRadius: "50%",
    background: "rgba(0,0,0,.55)",
    backdropFilter: "blur(6px)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    boxShadow: "0 8px 25px rgba(0,0,0,.45)",
    border: "1px solid rgba(255,255,255,.18)",
    transition: "all 0.2s ease",
  }}
>
  <div
    style={{
      width: 0,
      height: 0,
      borderLeft: "16px solid white",
      borderTop: "10px solid transparent",
      borderBottom: "10px solid transparent",
      marginLeft: 4, // visually center triangle
      opacity: 0.95,
    }}
  />
</div>
                </div>
              )}
            </div>
 
            {/* Poster-kort som överlappar till höger */}
            <div
              className="position-absolute end-0"
              style={{
                bottom: 12,
                width: 110,
                borderRadius: 10,
                overflow: "hidden",
                border: "1px solid rgba(255,255,255,.12)",
                boxShadow: "0 14px 30px rgba(0,0,0,.35)",
                background: "#111",
                transform: "translateX(8px)",
              }}
            >
              <img
                src={poster}
                alt={film.title}
                className="d-block w-100"
                style={{ objectFit: "cover", aspectRatio: "2 / 3" }}
              />
            </div>
          </div>
        </section>
 
        {/* behåll dina ändringar */}
        <div className="p-4 h-100">
          <h1 className="h2 mb-2">{film.title}</h1>
 
          <p className="text-muted mb-3">
            {film.production_year} | {film.genre} | {formatDuration(film.duration_minutes)} |{" "}
            {film.age_rating}+
          </p>
 
          <p className="mb-4" style={{ lineHeight: 1.6 }}>
            {film.description}
          </p>
 
          <div className="mb-4">
            <div className="mb-2">
              <span className="fw-semibold">Regissör:</span>{" "}
              <span className="text-muted">{directorNames}</span>
            </div>
 
            <div className="mb-2">
              <span className="fw-semibold">Rollista:</span>{" "}
              <span className="text-muted">{actorNames}</span>
            </div>
 
            <div className="mt-3">
              <div className="fw-semibold mb-2">Föreställningar:</div>
              <div className="d-flex flex-wrap gap-2">
                {showtimes.map((t) => (
                  <span key={t} className="badge text-bg-light border">
                    {t}
                  </span>
                ))}
              </div>
            </div>
          </div>
 
          <div className="d-flex flex-wrap gap-2">
            <button className="btn btn-danger fw-bold flex-grow-1 flex-md-grow-0">
              Boka biljetter
            </button>
            <button className="btn btn-outline-secondary fw-bold flex-grow-1 flex-md-grow-0">
              Välj platser
            </button>
          </div>
        </div>
      </div>
 
      {/* ✅ Desktop/Tablet (md+): behåll din nuvarande layout (poster + info), trailer under */}
      <div className="d-none d-md-block">
        <div className="row g-4 align-items-stretch">
          <div className="col-12 col-md-4 col-lg-3">
            <div className="border rounded overflow-hidden h-100 bg-light">
              <img
                src={poster}
                alt={film.title}
                className="w-100 h-100 d-block"
                style={{ objectFit: "cover" }}
              />
            </div>
          </div>
 
          <div className="col-12 col-md-8 col-lg-6">
            <div className=" p-4 h-100">
              <h1 className="h2 mb-2">{film.title}</h1>
 
              <p className="text-muted mb-3">
                {film.production_year} | {film.genre} | {formatDuration(film.duration_minutes)} |{" "}
                {film.age_rating}+
              </p>
 
              <p className="mb-4" style={{ lineHeight: 1.6 }}>
                {film.description}
              </p>
 
              <div className="mb-4">
                <div className="mb-2">
                  <span className="fw-semibold">Regissör:</span>{" "}
                  <span className="text-muted">{directorNames}</span>
                </div>
 
                <div className="mb-2">
                  <span className="fw-semibold">Rollista:</span>{" "}
                  <span className="text-muted">{actorNames}</span>
                </div>
 
                <div className="mt-3">
                  <div className="fw-semibold mb-2">Föreställningar:</div>
                  <div className="d-flex flex-wrap gap-2">
                    {showtimes.map((t) => (
                      <span key={t} className="badge text-bg-light border">
                        {t}
                      </span>
                    ))}
                  </div>
                </div>
              </div>
 
              <div className="d-flex flex-wrap gap-2">
                <button className="btn btn-danger fw-bold flex-grow-1 flex-md-grow-0">
                  Boka biljetter
                </button>
                <button className="btn btn-outline-secondary fw-bold flex-grow-1 flex-md-grow-0">
                  Välj platser
                </button>
              </div>
            </div>
          </div>
        </div>
 
        <section className="mt-5">
          <h2 className="h5 mb-3 text-center text-md-start">Se trailer</h2>
 
          <div
            className="ratio ratio-16x9 border rounded bg-light overflow-hidden"
            style={{ maxWidth: 900, margin: "0 auto" }}
          >
            {trailerEmbedUrl ? (
              <iframe
                src={trailerEmbedUrl}
                title={`${film.title} Trailer`}
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            ) : (
              <div className="d-flex align-items-center justify-content-center">
                <span className="text-muted">Ingen trailer tillgänglig.</span>
              </div>
            )}
          </div>
        </section>
      </div>
 
      {/* ✅ Modal: alltid centrerad + mörk stil (inte vit) */}
      {isTrailerOpen && trailerAutoplayUrl && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label="Trailer"
          className="position-fixed top-0 start-0 w-100 h-100"
          style={{
            background: "rgba(0,0,0,.78)",
            zIndex: 1050,
            display: "grid",
            placeItems: "center",
            padding: 16,
          }}
          onClick={() => setIsTrailerOpen(false)}
        >
          <div
            className="rounded-3 overflow-hidden"
            style={{
              width: "min(920px, 100%)",
              background: "#0b0f17",
              boxShadow: "0 30px 90px rgba(0,0,0,.55)",
              border: "1px solid rgba(255,255,255,.10)",
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div
              className="d-flex align-items-center justify-content-between px-3 py-2"
              style={{ borderBottom: "1px solid rgba(255,255,255,.10)" }}
            >
              <div className="fw-semibold" style={{ color: "rgba(255,255,255,.9)" }}>
                Trailer – {film.title}
              </div>
              <button
                type="button"
                className="btn btn-sm btn-outline-light"
                style={{ borderColor: "rgba(255,255,255,.35)" }}
                onClick={() => setIsTrailerOpen(false)}
              >
                Stäng
              </button>
            </div>
 
            <div className="ratio ratio-16x9">
              <iframe
                src={trailerAutoplayUrl}
                title={`${film.title} Trailer`}
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}