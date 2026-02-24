interface MovieInfoCardProps {
  title: string;
  posterUrl?: string;
  duration?: string;
  genre?: string;
  description?: string;
  showtime?: string;
  salongName?: string;
}

export default function MovieInfoCard({
  title,
  posterUrl,
  duration,
  genre,
  description,
  showtime,
  salongName
}: MovieInfoCardProps) {
  return (
    <div className="ch-movie-info-card">
      <div className="ch-movie-info-layout">
        {posterUrl && (
          <div className="ch-movie-poster">
            <img src={posterUrl} alt={title} />
          </div>
        )}
        <div className="ch-movie-details">
          <h2 className="ch-movie-title">{title}</h2>
          <div className="ch-movie-meta">
            {duration && <span>{duration}</span>}
            {duration && genre && <span className="ch-meta-dot">•</span>}
            {genre && <span>{genre}</span>}
          </div>
          {showtime && (
            <div className="ch-movie-showtime">
              <span className="ch-showtime-label">Visning:</span>
              <span>{showtime}</span>
              {salongName && <span> | {salongName}</span>}
            </div>
          )}
          {description && (
            <p className="ch-movie-description">{description}</p>
          )}
        </div>
      </div>
    </div>
  );
}
