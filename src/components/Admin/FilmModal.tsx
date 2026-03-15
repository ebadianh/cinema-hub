import { useState } from "react";

type FilmModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onSave: () => void;
};

export default function FilmModal({ isOpen, onClose, onSave }: FilmModalProps) {
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    genre: "Krim",
    age_rating: "15",
    duration_minutes: "",
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setSaving(true);

    // Validering
    if (!formData.title.trim()) {
      setError("Titel är obligatorisk");
      setSaving(false);
      return;
    }

    if (!formData.duration_minutes || parseInt(formData.duration_minutes) <= 0) {
      setError("Längd måste vara ett positivt tal");
      setSaving(false);
      return;
    }

    try {
      const res = await fetch("/api/admin/films", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({
          ...formData,
          duration_minutes: parseInt(formData.duration_minutes),
          images: "[]",
          trailers: "[]",
        }),
      });

      const data = await res.json();

      if (!res.ok || data.error) {
        setError(data?.error || "Kunde inte skapa film");
        setSaving(false);
        return;
      }

      // Success!
      alert("Film skapad!");
      onSave(); // Uppdatera listan
      onClose(); // Stäng modal

      // Återställ formulär
      setFormData({
        title: "",
        description: "",
        genre: "Krim",
        age_rating: "15",
        duration_minutes: "",
      });
    } catch (err) {
      setError("Ett fel uppstod");
    } finally {
      setSaving(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className="modal show d-block"
      style={{ backgroundColor: "rgba(0,0,0,0.7)" }}
      onClick={onClose}
    >
      <div
        className="modal-dialog modal-dialog-centered"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-content bg-dark text-light border-0">
          {/* Header */}
          <div className="modal-header" style={{ borderBottom: "1px solid var(--ch-border)" }}>
            <h5 className="modal-title">Lägg till ny film</h5>
            <button
              type="button"
              className="btn-close btn-close-white"
              onClick={onClose}
            ></button>
          </div>

          {/* Body */}
          <form onSubmit={handleSubmit}>
            <div className="modal-body">
              {error && (
                <div className="alert alert-danger" role="alert">
                  {error}
                </div>
              )}

              {/* Titel */}
              <div className="mb-3">
                <label className="form-label">Titel *</label>
                <input
                  type="text"
                  className="form-control bg-dark text-light"
                  name="title"
                  value={formData.title}
                  onChange={handleChange}
                  style={{ border: "1px solid var(--ch-border)" }}
                  required
                />
              </div>

              {/* Beskrivning */}
              <div className="mb-3">
                <label className="form-label">Beskrivning</label>
                <textarea
                  className="form-control bg-dark text-light"
                  name="description"
                  rows={3}
                  value={formData.description}
                  onChange={handleChange}
                  style={{ border: "1px solid var(--ch-border)" }}
                ></textarea>
              </div>

              {/* Genre */}
              <div className="mb-3">
                <label className="form-label">Genre *</label>
                <select
                  className="form-select bg-dark text-light"
                  name="genre"
                  value={formData.genre}
                  onChange={handleChange}
                  style={{ border: "1px solid var(--ch-border)" }}
                  required
                >
                  <option value="Krim">Krim</option>
                  <option value="Action">Action</option>
                  <option value="Drama">Drama</option>
                  <option value="Thriller">Thriller</option>
                  <option value="Komedi">Komedi</option>
                  <option value="Sci-Fi">Sci-Fi</option>
                  <option value="Skräck">Skräck</option>
                </select>
              </div>

              <div className="row">
                {/* Åldersgräns */}
                <div className="col-md-6 mb-3">
                  <label className="form-label">Åldersgräns *</label>
                  <select
                    className="form-select bg-dark text-light"
                    name="age_rating"
                    value={formData.age_rating}
                    onChange={handleChange}
                    style={{ border: "1px solid var(--ch-border)" }}
                    required
                  >
                    <option value="Barntillåten">Barntillåten</option>
                    <option value="7">7</option>
                    <option value="11">11</option>
                    <option value="15">15</option>
                    <option value="18">18</option>
                  </select>
                </div>

                {/* Längd */}
                <div className="col-md-6 mb-3">
                  <label className="form-label">Längd (minuter) *</label>
                  <input
                    type="number"
                    className="form-control bg-dark text-light"
                    name="duration_minutes"
                    value={formData.duration_minutes}
                    onChange={handleChange}
                    style={{ border: "1px solid var(--ch-border)" }}
                    min="1"
                    required
                  />
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="modal-footer" style={{ borderTop: "1px solid var(--ch-border)" }}>
              <button
                type="button"
                className="btn ch-btn-outline"
                onClick={onClose}
                disabled={saving}
              >
                Avbryt
              </button>
              <button
                type="submit"
                className="btn ch-btn-primary"
                disabled={saving}
              >
                {saving ? "Sparar..." : "Spara film"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}