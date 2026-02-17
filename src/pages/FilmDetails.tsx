import React, { useState } from 'react'; 
export default function FilmDetails() {
  const trailerEmbedUrl: string | null = null;

  // State för att hantera om chatten är expanderad eller inte 

  const [isChatOpen, setIsChatOpen] = useState(false);
  const [message, setMessage] = useState("");

  return (
    <div className="container py-4">
      <div className="row g-4 align-items-start">
        
        {/* 1. Poster: Full width on mobile, 1/3 on tablets+ */}
        <div className="col-12 col-md-4 col-lg-3">
          <div
            className="border rounded bg-light d-flex align-items-center justify-content-center w-100"
            style={{ minHeight: "320px" }}
          >
            <span className="text-muted">Film­poster</span>
          </div>
        </div>

        {/* 2. Film Details: Full width on mobile, 2/3 on tablets, 1/2 on large desktop */}
        <div className="col-12 col-md-8 col-lg-6">
          <h1 className="h2 mb-2">Dune: Part Two</h1>
          <p className="text-muted mb-3">2024 | Sci-Fi / Äventyr | 2h 46m</p>
          <p className="mb-4">
            Paul Atreides unites with Chani and the Fremen...
          </p>
          {/* ... dl details ... */}
          <div className="d-flex flex-wrap gap-2 mt-4">
            <button className="btn btn-primary flex-grow-1 flex-md-grow-0">Boka Biljetter</button>
            <button className="btn btn-outline-secondary flex-grow-1 flex-md-grow-0">Välj Platser</button>
          </div>
        </div>

        {/* 3. AI Panel: Moves below the details on mobile, stays on the side on desktop */}
        {/* REMOVED 'd-none' so it's visible on all screens */}
        <div className="col-12 col-lg-3">
          <div className="card shadow-sm border-primary mt-4 mt-lg-0">
            <div className="card-header bg-primary text-white d-flex justify-content-between align-items-center">
              <span className="fw-semibold">Fråga AI:n</span>
              {isChatOpen && (
                <button onClick={() => setIsChatOpen(false)} className="btn btn-sm text-white border-0 p-0">×</button>
              )}
            </div>
            <div className="card-body">
              {!isChatOpen ? (
                <button onClick={() => setIsChatOpen(true)} className="btn btn-outline-primary w-100">
                  Öppna AI-chatt
                </button>
              ) : (
                <div className="d-flex flex-column" style={{ height: "300px" }}>
                   {/* message display */}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* 4. Trailer Section: Takes more width on mobile for better viewing */}
      <section className="mt-5">
        <h2 className="h5 mb-3 text-center text-md-start">Se Trailer</h2>
        <div 
            className="ratio ratio-16x9 border rounded overflow-hidden" 
            style={{ width: "100%", maxWidth: "900px", margin: "0 auto" }}
        >
          <div className="bg-light d-flex align-items-center justify-content-center w-100 h-100">
             <button className="btn btn-outline-secondary rounded-circle px-3 py-2">▶</button>
          </div>
        </div>
      </section>
    </div>
  );
}