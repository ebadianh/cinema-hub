import { Button } from "react-bootstrap";
import { Link } from "react-router-dom";

export default function Contact() {
  return (
    <div className="container py-5 text-light">

      <div className="mb-5 text-center text-md-start">
        <h1 className="mb-3">Kontakta familjen</h1>

        <p className="lead">Behöver du hjälp med en bokning eller har frågor om CinemaMob?
          Familjen tar hand om dig.</p>
        <Button
          variant="danger"
          onClick={() => window.dispatchEvent(new Event("open-ai-chat"))}
        >
          Prata med gudfadern
        </Button>
      </div>
    </div>



  );
}