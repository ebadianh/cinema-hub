export default function Hero() {
  return (
    <section className="py-4">
      <div className="container">
        <div className="ch-hero position-relative overflow-hidden">
          {/* Overlay */}
          <div className="ch-hero__overlay" />

          {/* Content */}
          <div className="position-relative text-center py-5 px-3 px-md-5">
            <h1 className="display-5 fw-bold mb-2">CinemaHub</h1>
            <p className="lead mb-0 ch-muted">
              Discover movies. Book seats. Experience cinema differently.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}
