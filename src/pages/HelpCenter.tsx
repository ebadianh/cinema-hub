export default function HelpCenter() {
  return (
    <section className="container py-5">
      <div className="mb-5 text-center">
        <h1 className="fw-bold mb-3">Hjälpcenter</h1>
        <p className="ch-muted mx-auto" style={{ maxWidth: "700px" }}>
          Välkommen till CinemaHubs hjälpcenter. Här hittar du svar på vanliga
          frågor och information om hur du får support.
        </p>
      </div>

      <div className="row g-4">
        <div className="col-md-6">
          <div className="card ch-card h-100">
            <div className="card-body">
              <h3 className="card-title mb-3">Vanliga frågor</h3>
              <ul className="ch-muted">
                <li>
                  <strong>Hur bokar jag biljetter?</strong>
                  <br />
                  Välj en film, välj visning och följ stegen i bokningsflödet
                  för att välja platser och slutföra betalning.
                </li>
                <li className="mt-3">
                  <strong>Hur avbokar jag en biljett?</strong>
                  <br />
                  Regler för avbokning kan läggas in här. Tills vidare kan du
                  kontakta kundsupport för hjälp.
                </li>
                <li className="mt-3">
                  <strong>Jag har inte fått min bokningsbekräftelse.</strong>
                  <br />
                  Kontrollera skräpposten i din e‑post. Om du fortfarande inte
                  hittar bekräftelsen kan du kontakta oss via kontaktsidan.
                </li>
              </ul>
            </div>
          </div>
        </div>

        <div className="col-md-6">
          <div className="card ch-card h-100">
            <div className="card-body">
              <h3 className="card-title mb-3">Behöver du mer hjälp?</h3>
              <p className="ch-muted">
                Om du inte hittar svaret på din fråga här kan du alltid höra av
                dig till vår kundsupport via sidan &quot;Kontakt&quot; eller
                prata med vår AI‑Gudfader i chatten.
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

