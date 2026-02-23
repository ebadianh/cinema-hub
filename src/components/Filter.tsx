type FilterProps = {
  selectedAge: string;
  selectedGenre: string;
  onAgeChange: (age: string) => void;
  onGenreChange: (genre: string) => void;
  filteredCount: number;
  totalCount: number;
};

export default function Filter({
  selectedAge,
  selectedGenre,
  onAgeChange,
  onGenreChange,
  filteredCount,
  totalCount,
}: FilterProps) {

  return (
    <>

      {/* Desktop filter */}
      <div className="d-none d-lg-flex justify-content-between align-items-end mb-4">
        <div className="d-flex gap-3">
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
            <label htmlFor="ageGenreFilter" className="form-label small text-muted">
              Genre
            </label>
            <select
              id="ageGenreFilter"
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
        </div>

        {/* Antal filmer */}
        <div className="text-muted small pb-2">
          {filteredCount} av {totalCount} filmer
        </div>
      </div>

      {/* Mobile and tablet */}
      <div className="d-lg-none mb-4">
        <div className="row g-3">
          <div className="col-6">
            <label htmlFor="ageFilterMobile" className="form-label small text-muted">Åldersgräns</label>
            <select
              id="ageFilterMobile" // åldersgräns-filter
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
          <div className="col-6">
            <label htmlFor="genreFilterMobile" className="form-label small text-muted">Genre</label>
            <select
              id="genreFilterMobile" // genre-filter
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
        </div>
      </div>
    </>
  );
}