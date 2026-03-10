export default function About() {
  return (
    <section className="container py-5">
      
      <div className="mb-5 text-center">
        <h1 className="fw-bold mb-3">Om CinemaHub</h1>
        <p className="ch-muted mx-auto" style={{ maxWidth: "700px" }}>
          Fem kompisar från Södra Sandby med en dröm: att skapa den perfekta 
          biografupplevelsen. Efter år av filmkvällar och diskussioner om vad 
          som saknades i biovärlden, flyttade vi till Malmö 2024 och gjorde 
          verklighet av visionen.
        </p>
      </div>

      <div className="mb-5">
        <h2 className="fw-bold mb-4 text-center">Vår resa</h2>
        <div className="row g-4">
          <div className="col-md-4">
            <div className="card ch-card h-100">
              <div className="card-body text-center">
                <h3 className="mb-3">2023 - Södra Sandby</h3>
                <p className="ch-muted">
                  Det började med fem kompisar som försökte boka biljetter 
                  till Dune. Hemsidan kraschade, biljetterna tog slut. 
                  "Vi kan göra bättre själva", sa någon. Skämtet blev verklighet.
                </p>
              </div>
            </div>
          </div>

          <div className="col-md-4">
            <div className="card ch-card h-100">
              <div className="card-body text-center">
                <h3 className="mb-3">2024 - Flytten</h3>
                <p className="ch-muted">
                  Skämtet blev allvar. Efter månader av kodande, planering 
                  och mer kaffe än vad som är hälsosamt, packade vi väskorna 
                  och flyttade till Malmö med en vision.
                </p>
              </div>
            </div>
          </div>

          <div className="col-md-4">
            <div className="card ch-card h-100">
              <div className="card-body text-center">
                <h3 className="mb-3">Idag - CinemaHub</h3>
                <p className="ch-muted">
                  Två salonger, ett bokningssystem som faktiskt fungerar, 
                  och en passion för att göra biobesök roligare, enklare 
                  och mer minnesvärt. Sandby till Malmö — vi gjorde det.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="row g-4 mb-5">
        <div className="col-md-6">
          <div className="card ch-card h-100">
            <div className="card-body">
              <h3 className="card-title mb-3">Stora Salongen</h3>
              <p className="ch-muted">
                Vår stora salong erbjuder en förstklassig bioupplevelse med
                modern surroundteknik, högupplöst projektor och bekväma
                sittplatser designade för maximal komfort.
              </p>
              <ul className="ch-muted">
                <li>✔ Kapacitet: 48 platser</li>
                <li>✔ 7 rader med varierad bredd</li>
                <li>✔ Dolby surroundljud</li>
                <li>✔ Perfekt för premiärer och storfilmer</li>
              </ul>
            </div>
          </div>
        </div>

        <div className="col-md-6">
          <div className="card ch-card h-100">
            <div className="card-body">
              <h3 className="card-title mb-3">Lilla Salongen</h3>
              <p className="ch-muted">
                Den lilla salongen erbjuder en mer intim bioupplevelse och
                passar perfekt för familjefilmer, indieproduktioner och
                specialvisningar.
              </p>
              <ul className="ch-muted">
                <li>✔ Kapacitet: 20 platser</li>
                <li>✔ 4 rader med 5 platser per rad</li>
                <li>✔ Mysig och avslappnad miljö</li>
                <li>✔ Perfekt för klassiker och smalare filmer</li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      <div className="mb-5">
        <h2 className="fw-bold mb-4 text-center">Biljettpriser</h2>
        <div className="row g-4">
          <div className="col-md-4">
            <div className="card ch-card h-100 text-center">
              <div className="card-body">
                <h5 className="card-title">Vuxen</h5>
                <p className="display-4 fw-bold text-white">140 kr</p>
                <p className="ch-muted small">Standardpris från 12 år</p>
              </div>
            </div>
          </div>

          <div className="col-md-4">
            <div className="card ch-card h-100 text-center">
              <div className="card-body">
                <h5 className="card-title">Pensionär</h5>
                <p className="display-4 fw-bold text-white">120 kr</p>
                <p className="ch-muted small">Gäller med giltig legitimation</p>
              </div>
            </div>
          </div>

          <div className="col-md-4">
            <div className="card ch-card h-100 text-center">
              <div className="card-body">
                <h5 className="card-title">Barn</h5>
                <p className="display-4 fw-bold text-white">80 kr</p>
                <p className="ch-muted small">Under 12 år</p>
              </div>
            </div>
          </div>
        </div>
      </div>

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

      <div className="text-center mb-5">
        <h2 className="fw-bold mb-3">Kontakta oss</h2>
        <div className="ch-muted mx-auto" style={{ maxWidth: "500px" }}>
          <p className="mb-2">📍 Biografgatan 1, Malmö</p>
          <p className="mb-2">📧 info@cinemahub.se</p>
          <p className="mb-2">📞 010-123 45 67</p>
          <p className="mt-3 small">Öppettider: Vardagar 17:00-23:00, Helger 13:00-23:00</p>
        </div>
      </div>

      <div className="text-center">
        <h2 className="fw-bold mb-3">Vår mission</h2>
        <p className="ch-muted mx-auto" style={{ maxWidth: "700px" }}>
          Vi är fortfarande samma fem killar från Sandby, fast nu med en 
          biograf i Malmö och en mission: att bevisa att småstadsgrabbar kan 
          skapa något stort. Varje visning, varje besökare påminner oss om 
          varför vi började — för kärleken till film och gemenskapen den skapar.
        </p>
      </div>

    </section>
  );
}