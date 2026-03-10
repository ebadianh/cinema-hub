import { useState } from "react";
import type { FormEvent } from "react";

type ContactFormData = {
  name: string;
  email: string;
  subject: string;
  message: string;
};

export default function ContactForm() {
  const [formData, setFormData] = useState<ContactFormData>({
    name: "",
    email: "",
    subject: "",
    message: "",
  });

  const [status, setStatus] = useState<"idle" | "sending" | "success" | "error">("idle");
  const [errorMessage, setErrorMessage] = useState<string>("");

  // Hantera ändringar i formulärfält
  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  // Hantera formulär-submit
  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setStatus("sending");
    setErrorMessage("");

    try {
      const response = await fetch("/api/contacts", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(formData),
      });

      if (!response.ok) {
        throw new Error("Något gick fel, Familjen kunde inte ta emot ditt meddelande.");
      }

      setStatus("success");
      // Rensa formuläret
      setFormData({
        name: "",
        email: "",
        subject: "",
        message: "",
      });

      // Återställ success-meddelande efter 5 sekunder
      setTimeout(() => {
        setStatus("idle");
      }, 5000);
    } catch (error) {
      setStatus("error");
      setErrorMessage(error instanceof Error ? error.message : "Ett fel uppstod");
    }
  };

  return (
    <div className="card bg-dark text-light border-0 shadow">
      <div className="card-body p-4 p-md-5">
        <h3 className="mb-4">Skicka ett meddelande till Familjen</h3>

        {status === "success" && (
          <div className="alert alert-success mb-4">
            Tack. Familjen har tagit emot ditt meddelande. Vi hör av oss när tiden är rätt.
          </div>
        )}

        {status === "error" && (
          <div className="alert alert-danger mb-4">
            {errorMessage}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          {/* Namn */}
          <div className="mb-3">
            <label htmlFor="name" className="form-label">
              Namn <span className="text-danger">*</span>
            </label>
            <input
              type="text"
              id="name"
              name="name"
              className="form-control"
              value={formData.name}
              onChange={handleChange}
              required
              disabled={status === "sending"}
            />
          </div>

          {/* E-post */}
          <div className="mb-3">
            <label htmlFor="email" className="form-label">
              E-post <span className="text-danger">*</span>
            </label>
            <input
              type="email"
              id="email"
              name="email"
              className="form-control"
              value={formData.email}
              onChange={handleChange}
              required
              disabled={status === "sending"}
            />
          </div>

          {/* Ämne */}
          <div className="mb-3">
            <label htmlFor="subject" className="form-label">
              Ämne <span className="text-danger">*</span>
            </label>
            <input
              type="text"
              id="subject"
              name="subject"
              className="form-control"
              value={formData.subject}
              onChange={handleChange}
              required
              disabled={status === "sending"}
            />
          </div>

          {/* Meddelande */}
          <div className="mb-4">
            <label htmlFor="message" className="form-label">
              Meddelande <span className="text-danger">*</span>
            </label>
            <textarea
              id="message"
              name="message"
              className="form-control"
              rows={5}
              value={formData.message}
              onChange={handleChange}
              required
              disabled={status === "sending"}
            />
          </div>

          {/* Submit-knapp */}
          <button
            type="submit"
            className="btn btn-primary w-100"
            disabled={status === "sending"}
          >
            {status === "sending" ? "Skickar..." : "Skicka meddelande"}
          </button>
        </form>
      </div>
    </div>
  );
}