import { useState, useEffect } from "react";

type FilmModalProps = {
  isOpen: boolean;
  onClose: () => void;
  onSave: () => void;
  film?: {
    id: number;
    title: string;
    description: string;
    duration_minutes: number;
    age_rating: string;
    genre: string;
    images: string[];
    trailers: string[];
  } | null;
};

export default function FilmModal({ isOpen, onClose, onSave, film }: FilmModalProps) {
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    genre: "Krim",
    age_rating: "15",
    duration_minutes: "",
  });
  const [images, setImages] = useState<string[]>([""]);
  const [trailers, setTrailers] = useState<string[]>([""]);

  // Directors & Actors
  const [availableDirectors, setAvailableDirectors] = useState<string[]>([]);
  const [availableActors, setAvailableActors] = useState<string[]>([]);
  const [selectedDirectors, setSelectedDirectors] = useState<string[]>([]);
  const [selectedActors, setSelectedActors] = useState<string[]>([]);
  const [newDirectorName, setNewDirectorName] = useState("");
  const [newActorName, setNewActorName] = useState("");

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  // Hämta directors och actors när modal öppnas
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = "hidden";

      fetchDirectorsAndActors();
      if (film) {
        setFormData({
          title: film.title,
          description: film.description,
          genre: film.genre,
          age_rating: film.age_rating,
          duration_minutes: film.duration_minutes.toString(),
        });

        // Parsar images och trailers (de är JSON-strängar från backend)
        try {
          const parsedImages = typeof film.images === 'string'
            ? JSON.parse(film.images)
            : film.images;
          const parsedTrailers = typeof film.trailers === 'string'
            ? JSON.parse(film.trailers)
            : film.trailers;

          setImages(parsedImages.length > 0 ? parsedImages : [""]);
          setTrailers(parsedTrailers.length > 0 ? parsedTrailers : [""]);
        } catch (e) {
          setImages([""]);
          setTrailers([""]);
        }

        fetchFilmDirectorsAndActors(film.id);
      } else {
        resetForm();
      }
    }

    return () => {
      document.body.style.overflow = "auto";
    };
  }, [isOpen, film]);

  // Reset-funktion
  const resetForm = () => {
    setFormData({
      title: "",
      description: "",
      genre: "Krim",
      age_rating: "15",
      duration_minutes: "",
    });
    setImages([""]);
    setTrailers([""]);
    setSelectedDirectors([]);
    setSelectedActors([]);
    setNewDirectorName("");
    setNewActorName("");
  };

  const fetchFilmDirectorsAndActors = async (filmId: number) => {
    try {
      const [directorsRes, actorsRes] = await Promise.all([
        fetch("/api/directors", { credentials: "include" }),
        fetch("/api/actors", { credentials: "include" }),
      ]);

      const directorsData = await directorsRes.json();
      const actorsData = await actorsRes.json();

      // Filtrera directors/actors för denna specifika film
      const filmDirectors = directorsData
        .filter((d: any) => d.film_id === filmId)
        .map((d: any) => d.name);

      const filmActors = actorsData
        .filter((a: any) => a.film_id === filmId)
        .map((a: any) => a.name);

      setSelectedDirectors(filmDirectors);
      setSelectedActors(filmActors);
    } catch (err) {
      console.error("Kunde inte hämta film directors/actors:", err);
    }
  };

  const fetchDirectorsAndActors = async () => {
    try {
      const [directorsRes, actorsRes] = await Promise.all([
        fetch("/api/directors", { credentials: "include" }),
        fetch("/api/actors", { credentials: "include" }),
      ]);

      const directorsData = await directorsRes.json();
      const actorsData = await actorsRes.json();

      // Extrahera unika namn
      const uniqueDirectors = [...new Set(directorsData.map((d: any) => d.name))];
      const uniqueActors = [...new Set(actorsData.map((a: any) => a.name))];

      setAvailableDirectors(uniqueDirectors as string[]);
      setAvailableActors(uniqueActors as string[]);
    } catch (err) {
      console.error("Kunde inte hämta directors/actors:", err);
    }
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    });
  };

  const handleImageChange = (index: number, value: string) => {
    const newImages = [...images];
    newImages[index] = value;
    setImages(newImages);
  };

  const addImageField = () => {
    setImages([...images, ""]);
  };

  const removeImageField = (index: number) => {
    setImages(images.filter((_, i) => i !== index));
  };

  const handleTrailerChange = (index: number, value: string) => {
    const newTrailers = [...trailers];
    newTrailers[index] = value;
    setTrailers(newTrailers);
  };

  const addTrailerField = () => {
    setTrailers([...trailers, ""]);
  };

  const removeTrailerField = (index: number) => {
    setTrailers(trailers.filter((_, i) => i !== index));
  };

  const toggleDirector = (name: string) => {
    setSelectedDirectors((prev) =>
      prev.includes(name) ? prev.filter((d) => d !== name) : [...prev, name]
    );
  };

  const toggleActor = (name: string) => {
    setSelectedActors((prev) =>
      prev.includes(name) ? prev.filter((a) => a !== name) : [...prev, name]
    );
  };

  const addNewDirector = () => {
    const trimmed = newDirectorName.trim();
    if (trimmed && !availableDirectors.includes(trimmed)) {
      setAvailableDirectors([...availableDirectors, trimmed]);
      setSelectedDirectors([...selectedDirectors, trimmed]);
      setNewDirectorName("");
    }
  };

  const addNewActor = () => {
    const trimmed = newActorName.trim();
    if (trimmed && !availableActors.includes(trimmed)) {
      setAvailableActors([...availableActors, trimmed]);
      setSelectedActors([...selectedActors, trimmed]);
      setNewActorName("");
    }
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

    // Filtrera bort tomma URLs
    const validImages = images.filter((img) => img.trim() !== "");
    const validTrailers = trailers.filter((tr) => tr.trim() !== "");

    try {
      const isEditMode = !!film;

      // STEG 1: Skapa filmen
      const filmRes = await fetch(isEditMode ? `/api/admin/films/${film.id}` : "/api/admin/films",
        {
          method: isEditMode ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            ...formData,
            duration_minutes: parseInt(formData.duration_minutes),
            images: JSON.stringify(validImages),
            trailers: JSON.stringify(validTrailers),
          }),
        });

      const filmData = await filmRes.json();

      if (!filmRes.ok || filmData.error) {
        setError(filmData?.error || `Kunde inte ${isEditMode ? 'uppdatera' : 'skapa'} film`);
        setSaving(false);
        return;
      }

      // Hämta film-ID från response
      const filmId = isEditMode ? film.id : (filmData.id || filmData.insertId);

      if (!filmId) {
        setError("Kunde inte hämta film-ID");
        setSaving(false);
        return;
      }

      // STEG 2: Om edit mode - ta bort gamla directors/actors först
      if (isEditMode) {
        await fetch(`/api/admin/films/${filmId}/directors`, {
          method: "DELETE",
          credentials: "include",
        });

        await fetch(`/api/admin/films/${filmId}/actors`, {
          method: "DELETE",
          credentials: "include",
        });
      }

      // STEG 3: Lägg till directors (om några valda)
      if (selectedDirectors.length > 0) {
        await fetch(`/api/admin/films/${filmId}/directors`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ directors: selectedDirectors }),
        });
      }

      // STEG 4: Lägg till actors (om några valda)
      if (selectedActors.length > 0) {
        await fetch(`/api/admin/films/${filmId}/actors`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ actors: selectedActors }),
        });
      }

      // Success!
      alert(isEditMode ? "Film uppdaterad!" : "Film skapad!");
      onSave();
      onClose();
      resetForm();

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
        className="modal-dialog modal-dialog-centered modal-lg"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-content bg-dark text-light border-0">
          {/* Header */}
          <div className="modal-header" style={{ borderBottom: "1px solid var(--ch-border)" }}>
            <h5 className="modal-title">{film ? "Redigera film" : "Lägg till ny film"}</h5>
            <button
              type="button"
              className="btn-close btn-close-white"
              onClick={onClose}
            ></button>
          </div>

          {/* Body */}
          <form onSubmit={handleSubmit}>
            <div className="modal-body" style={{ maxHeight: "70vh", overflowY: "auto" }}>
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

              {/* DIRECTORS */}
              <div className="mb-3">
                <label className="form-label">Regissörer</label>
                <div
                  className="p-2"
                  style={{
                    border: "1px solid var(--ch-border)",
                    borderRadius: "8px",
                    maxHeight: "150px",
                    overflowY: "auto",
                    background: "var(--ch-surface)",
                  }}
                >
                  {availableDirectors.map((director) => (
                    <div key={director} className="form-check">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        id={`director-${director}`}
                        checked={selectedDirectors.includes(director)}
                        onChange={() => toggleDirector(director)}
                      />
                      <label className="form-check-label" htmlFor={`director-${director}`}>
                        {director}
                      </label>
                    </div>
                  ))}
                </div>

                <div className="input-group mt-2">
                  <input
                    type="text"
                    className="form-control form-control-sm bg-dark text-light"
                    placeholder="Ny regissör..."
                    value={newDirectorName}
                    onChange={(e) => setNewDirectorName(e.target.value)}
                    onKeyPress={(e) => e.key === "Enter" && (e.preventDefault(), addNewDirector())}
                    style={{ border: "1px solid var(--ch-border)" }}
                  />
                  <button
                    type="button"
                    className="btn btn-sm ch-btn-outline"
                    onClick={addNewDirector}
                    disabled={!newDirectorName.trim()}
                  >
                    + Lägg till
                  </button>
                </div>

                <small className="text-muted">
                  Valda: {selectedDirectors.length > 0 ? selectedDirectors.join(", ") : "Inga"}
                </small>
              </div>

              {/* ACTORS */}
              <div className="mb-3">
                <label className="form-label">Skådespelare</label>
                <div
                  className="p-2"
                  style={{
                    border: "1px solid var(--ch-border)",
                    borderRadius: "8px",
                    maxHeight: "150px",
                    overflowY: "auto",
                    background: "var(--ch-surface)",
                  }}
                >
                  {availableActors.map((actor) => (
                    <div key={actor} className="form-check">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        id={`actor-${actor}`}
                        checked={selectedActors.includes(actor)}
                        onChange={() => toggleActor(actor)}
                      />
                      <label className="form-check-label" htmlFor={`actor-${actor}`}>
                        {actor}
                      </label>
                    </div>
                  ))}
                </div>

                <div className="input-group mt-2">
                  <input
                    type="text"
                    className="form-control form-control-sm bg-dark text-light"
                    placeholder="Ny skådespelare..."
                    value={newActorName}
                    onChange={(e) => setNewActorName(e.target.value)}
                    onKeyPress={(e) => e.key === "Enter" && (e.preventDefault(), addNewActor())}
                    style={{ border: "1px solid var(--ch-border)" }}
                  />
                  <button
                    type="button"
                    className="btn btn-sm ch-btn-outline"
                    onClick={addNewActor}
                    disabled={!newActorName.trim()}
                  >
                    + Lägg till
                  </button>
                </div>

                <small className="text-muted">
                  Valda: {selectedActors.length > 0 ? selectedActors.join(", ") : "Inga"}
                </small>
              </div>

              {/* BILDER */}
              <div className="mb-3">
                <label className="form-label d-flex justify-content-between align-items-center">
                  <span>Bilder (URLs)</span>
                  <button
                    type="button"
                    className="btn btn-sm ch-btn-outline"
                    onClick={addImageField}
                  >
                    + Lägg till bild
                  </button>
                </label>
                {images.map((img, index) => (
                  <div key={index} className="input-group mb-2">
                    <input
                      type="url"
                      className="form-control bg-dark text-light"
                      placeholder="https://example.com/poster.jpg"
                      value={img}
                      onChange={(e) => handleImageChange(index, e.target.value)}
                      style={{ border: "1px solid var(--ch-border)" }}
                    />
                    {images.length > 1 && (
                      <button
                        type="button"
                        className="btn btn-outline-danger"
                        onClick={() => removeImageField(index)}
                      >
                        ×
                      </button>
                    )}
                  </div>
                ))}
              </div>

              {/* TRAILERS */}
              <div className="mb-3">
                <label className="form-label d-flex justify-content-between align-items-center">
                  <span>Trailers (YouTube URLs)</span>
                  <button
                    type="button"
                    className="btn btn-sm ch-btn-outline"
                    onClick={addTrailerField}
                  >
                    + Lägg till trailer
                  </button>
                </label>
                {trailers.map((tr, index) => (
                  <div key={index} className="input-group mb-2">
                    <input
                      type="url"
                      className="form-control bg-dark text-light"
                      placeholder="https://www.youtube.com/watch?v=..."
                      value={tr}
                      onChange={(e) => handleTrailerChange(index, e.target.value)}
                      style={{ border: "1px solid var(--ch-border)" }}
                    />
                    {trailers.length > 1 && (
                      <button
                        type="button"
                        className="btn btn-outline-danger"
                        onClick={() => removeTrailerField(index)}
                      >
                        ×
                      </button>
                    )}
                  </div>
                ))}
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
              <button type="submit" className="btn ch-btn-primary" disabled={saving}>
                {saving ? "Sparar..." : film ? "Uppdatera film" : "Spara film"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};