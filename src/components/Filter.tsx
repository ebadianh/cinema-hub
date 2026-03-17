import DatePicker from "react-datepicker";
import { format, parseISO } from "date-fns";
import { sv } from "date-fns/locale";

type FilterProps = {
  selectedAge: string;
  selectedGenre: string;
  selectedDate?: string;
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
  onAgeChange,
  onGenreChange,
  onDateChange = () => { }, /* om ingen funktion skickas in, använd en tom funktion istället för att undvika att krascha */
  onReset = () => { },
  filteredCount,
  totalCount,
}: FilterProps) {
  const parseDateValue = (value: string) => parseISO(value);

  const handleDateChange = (value: string) => {
    onDateChange(value);
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
              <option value="Krim">Krim</option>
            </select>
          </div>

          {/* Datum - date picker */}
          <div style={{ minWidth: "200px" }}>
            <label htmlFor="dateFilter" className="form-label small text-muted">
              Datum
            </label>
            <DatePicker
              selected={selectedDate !== "all" ? parseDateValue(selectedDate) : null}
              onChange={(date: Date | null) => {
                if (date) {
                  const dateString = format(date, "yyyy-MM-dd");
                  handleDateChange(dateString);
                } else {
                  handleDateChange("all");
                }
              }}
              dateFormat="EEE d MMMM"
              locale={sv}
              placeholderText="Alla datum"
              className="form-control form-select"
              wrapperClassName="d-block"
              popperClassName="filter-datepicker-popper"
              popperPlacement="bottom-start"
            />
          </div>
        </div>

        {/* Reset och antal filmer till höger */}
        <div className="d-flex align-items-center gap-3">
          <button
            className="btn btn-outline-secondary btn-sm"
            onClick={onReset}
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

      {/* Mobile */}
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
            <option value="Krim">Krim</option>
          </select>
        </div>

        <div className="mb-3">
          <label htmlFor="datumFilterMobile" className="form-label small text-muted">
            Datum
          </label>
          <DatePicker
            selected={selectedDate !== "all" ? parseDateValue(selectedDate) : null}
            onChange={(date: Date | null) => {
              if (date) {
                const dateString = format(date, "yyyy-MM-dd");
                handleDateChange(dateString);
              } else {
                handleDateChange("all");
              }
            }}
            dateFormat="EEE d MMMM"
            locale={sv}
            placeholderText="Alla datum"
            className="form-control form-select"
            wrapperClassName="d-block"
            popperClassName="filter-datepicker-popper"
            popperPlacement="bottom"
          />
        </div>

        {/* Reset-knapp för mobil */}
        <button
          className="btn btn-outline-secondary btn-sm w-100"
          onClick={onReset}
          type="button"
        >
          Rensa filter
        </button>
      </div>
    </>
  );
}
