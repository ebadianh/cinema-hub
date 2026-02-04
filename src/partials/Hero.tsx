export default function Hero() {
  return (
    <section
      className="hero-section d-flex align-items-center text-white"
      style={{
        backgroundImage: "url('/hero.jpg')",
        backgroundSize: "cover",
        backgroundPosition: "center",
        minHeight: "60vh",
        position: "relative",
      }}
    >
      {/* Overlay */}
      <div
        style={{
          position: "absolute",
          inset: 0,
          backgroundColor: "rgba(0,0,0,0.55)",
        }}
      />

      {/* Content */}
      <div className="container position-relative text-center">
        <h1 className="display-3 fw-bold">CinemaHub</h1>

        <p className="lead mt-3">
          Discover movies. Book seats. Experience cinema differently.
        </p>
      </div>
    </section>
  );
}
