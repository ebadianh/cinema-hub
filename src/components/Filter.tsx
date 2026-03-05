import { useState } from "react";

type FilterProps = {
  selectedAge: string;
  selectedGenre: string;
  selectedDate?: string;
  availableDates?: string[];
  onAgeChange: (age: string) => void;
  onGenreChange: (genre: string) => void;
  onDateChange?: (date: string) => void;
  onReset?: () => void;
  filteredCount: number;
  totalCount: number;
};

export default function Filter({
  selectedAge,
  selectedGenre,
  selectedDate = "all",
  availableDates = [],
  onAgeChange,
  onGenreChange,
  onDateChange = () => { }, /* om ingen funktion skickas in, använd en tom funktion istället för att undvika att krascha */
  onReset = () => { },
  filteredCount,
  totalCount,
}: FilterProps) {

  const [showDatePicker, setShowDatePicker] = useState(false);

  const formatDateFull = (dateString: string) => {
    const date = new Date(dateString);
    const weekday = date.toLocaleDateString("sv-SE", { weekday: "long" });
    const day = date.getDate();
    const month = date.toLocaleDateString("sv-SE", { month: "long" });
    return `${weekday.charAt(0).toUpperCase() + weekday.slice(1)} ${day} ${month}`;
  };

  const handleDateChange = (value: string) => {
    onDateChange(value);
    setShowDatePicker(false);
  };

  return (
    <>

      {/* Desktop filter */}
      <div className="d-none d-lg-flex justify-content-between align-items-end mb-4">
        <div className="d-flex gap-3">
          {/* åldersgräns */}
          <div style={{ minWidth: '200px' }}>
            <label htmlFor="ageFilter" className="form-label small text-muted"> {/* åldersgräns-filter */}
              Åldersgräns
            </label>
            <select
              id="ageFilter"
              className="form-select"
              value={selectedAge}
              onChange={(e) => onAgeChange(e.target.value)}>
              <option value="all">Alla åldrar</option>
              <option value="0">Barntillåten</option>
              <option value="7">7+</option>
              <option value="11">11+</option>
              <option value="15">15+</option>
            </select>
          </div>

          {/* Genre-filter */}
          <div style={{ minWidth: '200px' }}>
            <label htmlFor="genreFilter" className="form-label small text-muted">
              Genre
            </label>
            <select
              id="genreFilter"
              className="form-select"
              value={selectedGenre}
              onChange={(e) => onGenreChange(e.target.value)}>
              <option value="all">Alla genrer</option>
              <option value="Drama">Drama</option>
              <option value="Action">Action</option>
              <option value="Komedi">Komedi</option>
              <option value="Sci-Fi">Sci-Fi</option>
              <option value="Animerat">Animerat</option>
              <option value="Thriller">Thriller</option>
              <option value="Skräck">Skräck</option>
            </select>
          </div>

          {/* Datum - toggle mellan dropdown och date picker */}
          <div style={{ minWidth: '200px', position: 'relative' }}>
            <label className="form-label small text-muted">Datum</label>
            {!showDatePicker ? (
              <div style={{ position: 'relative' }}>
                <button
                  type="button"
                  className="form-select d-flex align-items-center justify-content-between"
                  style={{ width: '100%', textAlign: 'left', paddingRight: '2.5rem' }}
                  onClick={() => setShowDatePicker(true)}
                >
                  <span>
                    {selectedDate === "all" ? "Alla datum" : formatDateFull(selectedDate)}
                  </span>
                  <span style={{ position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)' }}>
                    <svg width="20" height="20" fill="currentColor" viewBox="0 0 20 20">
                      <rect x="3" y="6" width="14" height="11" rx="2" fill="#888" />
                      <rect x="3" y="3" width="14" height="3" rx="1" fill="#bbb" />
                      <rect x="6" y="1" width="2" height="4" rx="1" fill="#888" />
                      <rect x="12" y="1" width="2" height="4" rx="1" fill="#888" />
                    </svg>
                  </span>
                </button>
              </div>
            ) : (
              <div>
                <input
                  type="date"
                  className="form-control"
                  value={selectedDate !== "all" ? selectedDate : ""}
                  onChange={(e) => {
                    if (e.target.value) {
                      handleDateChange(e.target.value);
                    }
                  }}
                  min={availableDates[0]}
                  max={availableDates[availableDates.length - 1]}
                />
              </div>
            )}
          </div>
        </div>

        {/* Reset och antal filmer till höger */}
        <div className="d-flex align-items-center gap-3">
          <button
            className="btn btn-outline-secondary btn-sm"
            onClick={() => {
              onReset();
              setShowDatePicker(false);
            }}
            type="button"
            style={{ whiteSpace: 'nowrap' }}
          >
            Rensa filter
          </button>
          <span className="text-muted small">
            {filteredCount} av {totalCount} filmer
          </span>
        </div>
      </div>

      {/* Mobile and tablet */}
      <div className="d-lg-none mb-4">
        <div className="mb-3">
          <label htmlFor="ageFilterMobile" className="form-label small text-muted">Åldersgräns</label>
          <select
            id="ageFilterMobile"
            className="form-select"
            value={selectedAge}
            onChange={(e) => onAgeChange(e.target.value)}
          >
            <option value="all">Alla åldrar</option>
            <option value="0">Barntillåten</option>
            <option value="7">7+</option>
            <option value="11">11+</option>
            <option value="15">15+</option>
          </select>
        </div>

        <div className="mb-3">
          <label htmlFor="genreFilterMobile" className="form-label small text-muted">Genre</label>
          <select
            id="genreFilterMobile"
            className="form-select"
            value={selectedGenre}
            onChange={(e) => onGenreChange(e.target.value)}
          >
            <option value="all">Alla genrer</option>
            <option value="Drama">Drama</option>
            <option value="Action">Action</option>
            <option value="Komedi">Komedi</option>
            <option value="Sci-Fi">Sci-Fi</option>
            <option value="Animerat">Animerat</option>
            <option value="Thriller">Thriller</option>
            <option value="Skräck">Skräck</option>
          </select>
        </div>

        <div className="mb-3">
          <label className="form-label small text-muted">Datum</label>

          {!showDatePicker ? (
            <div style={{ position: 'relative' }}>
              <button
                type="button"
                className="form-select d-flex align-items-center justify-content-between"
                style={{ width: '100%', textAlign: 'left', paddingRight: '2.5rem' }}
                onClick={() => setShowDatePicker(true)}
              >
                <span>
                  {selectedDate === "all" ? "Alla datum" : formatDateFull(selectedDate)}
                </span>
                <span style={{ position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)' }}>
                  <svg width="20" height="20" fill="currentColor" viewBox="0 0 20 20">
                    <rect x="3" y="6" width="14" height="11" rx="2" fill="#888" />
                    <rect x="3" y="3" width="14" height="3" rx="1" fill="#bbb" />
                    <rect x="6" y="1" width="2" height="4" rx="1" fill="#888" />
                    <rect x="12" y="1" width="2" height="4" rx="1" fill="#888" />
                  </svg>
                </span>
              </button>
            </div>
          ) : (
            <div>
              <input
                type="date"
                className="form-control"
                value={selectedDate !== "all" ? selectedDate : ""}
                onChange={(e) => {
                  if (e.target.value) {
                    handleDateChange(e.target.value);
                  }
                }}
                min={availableDates[0]}
                max={availableDates[availableDates.length - 1]}
              />
            </div>
          )}
        </div>

        {/* Reset-knapp för mobil */}
        <button
          className="btn btn-outline-secondary btn-sm w-100"
          onClick={() => {
            onReset();
            setShowDatePicker(false);
          }}
          type="button"
        >
          Rensa filter
        </button>
      </div>
    </>
  );
}