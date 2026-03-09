import { Button } from "react-bootstrap";
import { Link } from "react-router-dom";

export default function Contact() {
  return (
    <div className="container py-5 text-light">

      <div className="mb-5 text-center text-md-start">
        <h1 className="mb-3">Kontakta Familjen</h1>

        <p className="lead">Behöver du hjälp med en bokning eller har frågor om CinemaMob?
          Familjen tar hand om dig.
        </p>
      </div>

      {/* Gudfader kortet */}
      <div className="card bg-dark text-light border-0 shadow mb-5">
        <div className="card-body p-4 p-md-5 d-flex flex-column flex-md-row justify-content-between align-items-md-center">
          <div className="mb-3 mb-md-0">
            <p className="mb-0">För snabbast hjälp kan du prata direkt med vår AI‑Gudfader. Han kommer ge dig ett erbjudande du inte kan tacka nej till.</p>

            <Button
              variant="btn btn-primary"
              onClick={() => window.dispatchEvent(new Event("open-ai-chat"))}>Prata med gudfadern</Button>
          </div>

          <Link to="/chat" className="btn btn-primary mt-3 mt-md-0">Starta samtalet</Link>
        </div>
      </div>

      {/* Kontaktkort */}
      <div className="row g-4">

        {/* 1. Kundsupport */}
        <div className="col-12 col-md-6">
          <div className="card bg-dark text-light border-0 shadow h-100">
            <div className="card-body p-4">

              <h4 className="mb-3">Kundsupport</h4>

              <p>
                Maila oss om du har frågor om biljetter, bokningar eller tekniska problem.
              </p>

              <p className="mb-2">
                <strong>E-post</strong><br />
                support@cinemamob.com
              </p>

              <p className="mb-0">
                <strong>Telefon</strong><br />
                +46 70 111 11 11
              </p>
            </div>
          </div>
        </div>

        {/* 2. Affärer och samarbeten */}
        <div className="col-12 col-md-6">
          <div className="card bg-dark text-light border-0 shadow h-100">
            <div className="card-body p-4">

              <h4 className="mb-3">Affärer & Samarbeten</h4>

              <p>
                Vill du boka biografi för ett event, företagsvisning eller diskutera samarbeten?
              </p>

              <p className="mb-2">
                <strong>E-post</strong><br />
                business@cinemamob.com
              </p>

              <p className="mb-0">
                <strong>Telefon</strong><br />
                +46 70 222 22 22
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>




  );
}

