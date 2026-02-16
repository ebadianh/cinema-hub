import React, { useState } from 'react'; 


export default function FilmDetails() {
  const trailerEmbedUrl: string | null = null;

  // State för att hantera om chatten är expanderad eller inte 

  const [isChatOpen, setIsChatOpen] = useState(false);
  const [message, setMessage] = useState("");

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
          <p className="text-muted mb-3">2024 | Sci-Fi / Äventyr | 2h 46m</p>
          <p className="mb-4">
            Paul Atreides unites with Chani and the Fremen against the
            conspirators who destroyed his family...
          </p>

          <dl className="row small mb-0">
            <dt className="col-sm-3 fw-semibold">Regissör:</dt>
            <dd className="col-sm-9 mb-2">Denis Villeneuve</dd>
            {/* ... other details ... */}
          </dl>

          <div className="d-flex flex-wrap gap-2 mt-4">
            <button type="button" className="btn btn-primary">Boka Biljetter</button>
            <button type="button" className="btn btn-outline-secondary">Välj Platser</button>
          </div>
        </div>

        {/* AI side panel column - Now Dynamic */}
        <div className="col-lg-3 d-none d-lg-block">
          <div className="card shadow-sm border-primary">
            <div className="card-header bg-primary text-white d-flex justify-content-between align-items-center">
              <span className="fw-semibold">Fråga AI:n</span>
              {isChatOpen && (
                <button
                  onClick={() => setIsChatOpen(false)}
                  className="btn btn-sm text-white border-0 p-0"
                  style={{ fontSize: '1.2rem', lineHeight: 1 }}
                >
                  ×
                </button>
              )}
            </div>
            
            <div className="card-body">
              {!isChatOpen ? (
                /* Collapsed View */
                <>
                  <p className="small text-muted mb-3">
                    Ställ frågor om filmen, skådespelare eller visningstider.
                  </p>
                  <button 
                    onClick={() => setIsChatOpen(true)} 
                    className="btn btn-outline-primary w-100"
                  >
                    Öppna AI-chatt
                  </button>
                </>
              ) : (
                /* Expanded Chat View */
                <div className="d-flex flex-column" style={{ height: "300px" }}>
                  <div className="flex-grow-1 overflow-auto mb-3 p-2 bg-light rounded small">
                    <div className="mb-2 text-primary">AI: Hej! Vad vill du veta om Dune: Part Two?</div>
                  </div>
                  <div className="input-group">
                    <input 
                      type="text" 
                      className="form-control form-control-sm" 
                      placeholder="Skriv din fråga..."
                      value={message}
                      onChange={(e) => setMessage(e.target.value)}
                    />
                    <button className="btn btn-primary btn-sm" type="button">Sänd</button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Trailer section remains the same */}
      <section className="mt-5">
        <h2 className="h5 mb-3">Se Trailer</h2>
        <div className="ratio ratio-16x9 border rounded overflow-hidden" style={{ maxWidth: "80%", margin: "0 auto" }}>
          <div className="bg-light d-flex align-items-center justify-content-center w-100 h-100">
             <button className="btn btn-outline-secondary rounded-circle px-3 py-2">▶</button>
          </div>
        </div>
      </section>
    </div>
  );
}