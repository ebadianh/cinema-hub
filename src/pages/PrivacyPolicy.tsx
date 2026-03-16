export default function PrivacyPolicy() {
  return (
    <section className="container py-5">
      <div className="mb-5 text-center">
        <h1 className="fw-bold mb-3">Integritetspolicy</h1>
        <p className="ch-muted mx-auto" style={{ maxWidth: "700px" }}>
          Denna sida beskriver hur CinemaHub hanterar personuppgifter och
          integritet. Texten är en plats­hållare och kan uppdateras med en
          fullständig policy vid ett senare tillfälle.
        </p>
      </div>

      <div className="row g-4">
        <div className="col-md-6">
          <div className="card ch-card h-100">
            <div className="card-body">
              <h3 className="card-title mb-3">Vilka uppgifter vi samlar in</h3>
              <p className="ch-muted">
                Exempel på personuppgifter som kan behandlas är namn,
                kontaktuppgifter, bokningsinformation och teknisk information
                om hur du använder tjänsten.
              </p>
            </div>
          </div>
        </div>

        <div className="col-md-6">
          <div className="card ch-card h-100">
            <div className="card-body">
              <h3 className="card-title mb-3">Varför vi behandlar uppgifter</h3>
              <p className="ch-muted">
                Uppgifterna används för att hantera bokningar, förbättra
                tjänsten och ge dig en trygg och smidig bioupplevelse.
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="mt-5">
        <h2 className="fw-bold mb-3">Cookies & spårning</h2>
        <p className="ch-muted">
          CinemaHub kan använda cookies och liknande tekniker för att
          förbättra funktionalitet och analysera hur tjänsten används. Här kan
          du senare lägga in mer detaljerad information om olika typer av
          cookies och hur användaren kan hantera sina inställningar.
        </p>
      </div>

      <div className="mt-5">
        <h2 className="fw-bold mb-3">Kontakt</h2>
        <p className="ch-muted">
          Om du har frågor om hur vi hanterar personuppgifter kan du kontakta
          oss via de kontaktuppgifter som finns på kontaktsidan.
        </p>
      </div>
    </section>
  );
}

