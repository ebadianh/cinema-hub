export default function FilmDetails() {
  return (
    <div className="container py-4">
      <div className="row g-4 align-items-start">
        {/* Poster column */}
        <div className="col-lg-3 col-md-4">
          <div
            className="border rounded bg-light d-flex align-items-center justify-content-center w-100"
            style={{ minHeight: "320px" }}
          >
            <span className="text-muted">Film­poster</span>
          </div>
        </div>

        {/* Film details column */}
        <div className="col-lg-6 col-md-8">
          <h1 className="h2 mb-2">Dune: Part Two</h1>
          <p className="text-muted mb-3">
            2024 | Sci-Fi / Äventyr | 2h 46m
          </p>

          <p className="mb-4">
            Paul Atreides unites with Chani and the Fremen against the
            conspirators who destroyed his family, as he faces a choice between
            the love of his life and the fate of the known universe.
          </p>

          <dl className="row small mb-0">
            <dt className="col-sm-3 fw-semibold">Regissör:</dt>
            <dd className="col-sm-9 mb-2">Denis Villeneuve</dd>

            <dt className="col-sm-3 fw-semibold">Skådespelare:</dt>
            <dd className="col-sm-9 mb-2">
              Timothée Chalamet, Zendaya, Rebecca Ferguson, Josh Brolin m.fl.
            </dd>

            <dt className="col-sm-3 fw-semibold">Åldersgräns:</dt>
            <dd className="col-sm-9 mb-2">11 år</dd>

            <dt className="col-sm-3 fw-semibold">Visningstider:</dt>
            <dd className="col-sm-9 mb-0">
              <span className="badge text-bg-secondary me-2 mb-1">16:30</span>
              <span className="badge text-bg-secondary me-2 mb-1">19:15</span>
              <span className="badge text-bg-secondary me-2 mb-1">21:45</span>
            </dd>
          </dl>

          <div className="d-flex flex-wrap gap-2 mt-4">
            <button type="button" className="btn btn-primary">
              Boka Biljetter
            </button>
            <button type="button" className="btn btn-outline-secondary">
              Välj Platser
            </button>
          </div>
        </div>

        {/* AI side panel column */}
        <div className="col-lg-3 d-none d-lg-block">
          <div className="card shadow-sm">
            <div className="card-header d-flex justify-content-between align-items-center">
              <span className="fw-semibold">Fråga AI:n</span>
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary border-0 px-2 py-0"
                aria-label="Stäng AI-panel"
              >
                ×
              </button>
            </div>
            <div className="card-body">
              <p className="small text-muted mb-3">
                Ställ frågor om filmen, skådespelare eller visningstider.
              </p>
              <a href="/chat" className="btn btn-outline-primary w-100">
                Öppna AI-chatt
              </a>
            </div>
          </div>
        </div>
      </div>

      {/* Trailer section */}
      <section className="mt-5">
        <h2 className="h5 mb-3">Se Trailer</h2>
        <div className="ratio ratio-16x9 border rounded bg-light d-flex align-items-center justify-content-center">
          <button
            type="button"
            className="btn btn-outline-secondary rounded-circle px-3 py-2"
          >
            ▶
          </button>
        </div>
      </section>
    </div>
  );
}