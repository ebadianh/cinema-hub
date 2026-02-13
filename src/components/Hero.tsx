import { useEffect, useState } from "react";
import { Carousel } from "react-bootstrap";

type Film = {
  id: number;
  title: string;
  description: string;
  duration_minutes: number;
  age_rating: number;
  genre: string;
  images: string[];
};

export default function Hero() {
  const [films, setFilms] = useState<Film[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/films")
      .then((res) => res.json())
      .then((data) => {
        const filmsWithImages = data.filter(
          (f: Film) => f.images && f.images.length > 0
        );
        setFilms(filmsWithImages);
        setLoading(false);
      })
      .catch((err) => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  if (loading) {
    return (
      <section className="py-4">
        <div className="container">
          <div className="ch-hero position-relative overflow-hidden">
            <div className="ch-hero__overlay" />
            <div className="position-relative text-center py-5 px-3 px-md-5">
              <h1 className="display-5 fw-bold mb-2">CinemaHub</h1>
              <p className="lead mb-0 ch-muted">Laddar filmer...</p>
            </div>
          </div>
        </div>
      </section>
    );
  }

  if (films.length === 0) {
    return (
      <section className="py-4">
        <div className="container">
          <div className="ch-hero position-relative overflow-hidden">
            <div className="ch-hero__overlay" />
            <div className="position-relative text-center py-5 px-3 px-md-5">
              <h1 className="display-5 fw-bold mb-2">CinemaHub</h1>
              <p className="lead mb-0 ch-muted">
                Upptäck nya filmer, boka dina platser och upplev bio som aldrig förr.
              </p>
            </div>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="py-4">
      <div className="container">
        <Carousel
          className="ch-hero-carousel"
          interval={5000}
          pause="hover"
          indicators={true}
          controls={true}
        >
          {films.map((film) => (
            <Carousel.Item key={film.id}>
              <div className="ch-hero-slide">
                <div className="ch-hero__overlay" />
                <div className="ch-hero-layout d-none d-md-flex">
                  <div className="ch-hero-text">
                    <h1 className="display-4 fw-bold mb-3">{film.title}</h1>
                    <p className="lead mb-3 ch-muted">
                      {film.description?.substring(0, 150)}
                      {film.description && film.description.length > 150 ? "..." : ""}
                    </p>
                    <div className="d-flex gap-2 flex-wrap mb-4">
                      <span className="badge bg-secondary px-3 py-2">{film.genre}</span>
                      <span className="badge bg-dark px-3 py-2">{film.age_rating}+</span>
                      <span className="badge bg-dark px-3 py-2">
                        {Math.floor(film.duration_minutes / 60)} tim {film.duration_minutes % 60} min
                      </span>
                    </div>
                    <button className="btn ch-btn-primary btn-lg px-4">
                      Boka biljetter
                    </button>
                  </div>


                  <div className="ch-hero-poster-section">
                    <div className="ch-hero-poster">
                      <img src={film.images[0]} alt={film.title} />
                    </div>
                    <button className="btn btn-outline-light btn-lg mt-3 w-100">
                      Se mer
                    </button>
                  </div>
                </div>

                <div className="ch-hero-mobile d-md-none text-center py-4">
                  <h2 className="display-6 fw-bold mb-2">{film.title}</h2>
                  <div className="d-flex gap-2 justify-content-center mb-3">
                    <span className="badge bg-secondary px-2 py-1">{film.genre}</span>
                    <span className="badge bg-dark px-2 py-1">{film.age_rating}</span>
                  </div>
                  <button className="btn ch-btn-primary px-4">Boka biljetter</button>
                </div>
              </div>
            </Carousel.Item>
          ))}
        </Carousel>
      </div>
    </section>
  );
}