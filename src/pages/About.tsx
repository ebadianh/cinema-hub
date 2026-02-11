export default function About() {
  return (
    <section className="container py-5">
      
      {/* Header */}
      <div className="mb-5 text-center">
        <h1 className="fw-bold mb-3">Om CinemaHub</h1>
        <p className="ch-muted mx-auto" style={{ maxWidth: "700px" }}>
          CinemaHub är en modern biograf där teknik möter filmupplevelse.
          Vårt mål är att skapa minnesvärda biobesök för både filmälskare,
          familjer och vänner som vill uppleva film på bästa möjliga sätt.
        </p>
      </div>

      {/* Salonger */}
      <div className="row g-4 mb-5">
        <div className="col-md-6">
          <div className="card ch-card h-100">
            <div className="card-body">
              <h3 className="card-title mb-3">Stora salongen</h3>
              <p className="ch-muted">
                Vår stora salong erbjuder en förstklassig bioupplevelse med
                modern surroundteknik, högupplöst projektor och bekväma
                sittplatser designade för maximal komfort.
              </p>
              <ul className="ch-muted">
                <li>✔ Kapacitet: 120 platser</li>
                <li>✔ Dolby surroundljud</li>
                <li>✔ Extra benutrymme</li>
                <li>✔ Perfekt för premiärer och storfilmer</li>
              </ul>
            </div>
          </div>
        </div>

        <div className="col-md-6">
          <div className="card ch-card h-100">
            <div className="card-body">
              <h3 className="card-title mb-3">Lilla salongen</h3>
              <p className="ch-muted">
                Den lilla salongen erbjuder en mer intim bioupplevelse och
                passar perfekt för familjefilmer, indieproduktioner och
                specialvisningar.
              </p>
              <ul className="ch-muted">
                <li>✔ Kapacitet: 50 platser</li>
                <li>✔ Mysig och avslappnad miljö</li>
                <li>✔ Perfekt för klassiker och smalare filmer</li>
                <li>✔ Möjlighet till privata visningar</li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      {/* Snacks */}
      <div className="mb-5">
        <h2 className="fw-bold mb-4 text-center">Snacks & dryck</h2>

        <div className="row g-4">
          <div className="col-md-4">
            <div className="card ch-card h-100">
              <div className="card-body">
                <h5 className="card-title">Bioklassiker</h5>
                <ul className="ch-muted">
                  <li>Smörpopcorn</li>
                  <li>Salt popcorn</li>
                  <li>Godis & choklad</li>
                  <li>Nachos med ostsås</li>
                </ul>
              </div>
            </div>
          </div>

          <div className="col-md-4">
            <div className="card ch-card h-100">
              <div className="card-body">
                <h5 className="card-title">Drycker</h5>
                <ul className="ch-muted">
                  <li>Läsk & mineralvatten</li>
                  <li>Juice</li>
                  <li>Kaffe & te</li>
                  <li>Energidrycker</li>
                </ul>
              </div>
            </div>
          </div>

          <div className="col-md-4">
            <div className="card ch-card h-100">
              <div className="card-body">
                <h5 className="card-title">Premium snacks</h5>
                <ul className="ch-muted">
                  <li>Gourmetpopcorn</li>
                  <li>Chokladpraliner</li>
                  <li>Glass & desserter</li>
                  <li>Säsongsbaserade snacks</li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Extra info */}
      <div className="text-center">
        <h2 className="fw-bold mb-3">Vår vision</h2>
        <p className="ch-muted mx-auto" style={{ maxWidth: "700px" }}>
          Vi strävar efter att kombinera modern teknik med klassisk
          biokänsla. Hos CinemaHub vill vi att varje besök ska kännas
          speciellt — oavsett om du kommer för en premiär, familjekväll
          eller spontant biobesök.
        </p>
      </div>

    </section>
  );
}
