import { useEffect, useState } from "react";

type Showing = {
  id: number;
  film_id: number;
  salong_id: number;
  start_time: string;
  language: string;
  subtitle: string;
};

type DateFilterProps = {
  selectedDate: string;
  onDateChange: (date: string) => void;
};

export default function DateFilter({
  selectedDate,
  onDateChange,
}: DateFilterProps) {
  const [availableDates, setAvailableDates] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("/api/showings")
      .then((res) => res.json())
      .then((data: Showing[]) => {

        const dates = data.map((s) => s.start_time.split("T")[0]);
        const uniqueDates = [...new Set(dates)].sort();
        setAvailableDates(uniqueDates);
        setLoading(false);
      })
      .catch((err) => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    const weekday = date.toLocaleDateString("sv-SE", { weekday: "long" });
    const day = date.getDate();
    const month = date.toLocaleDateString("sv-SE", { month: "long" });
    return `${weekday.charAt(0).toUpperCase() + weekday.slice(1)} ${day} ${month}`;
  };

  if (loading) return null;

  return (
    <div className="row mb-3">
      <div className="col-md-6 col-lg-4">
        <label htmlFor="dateFilter" className="form-label small text-muted">
          Välj datum
        </label>
        <select
          id="dateFilter"
          className="form-select"
          value={selectedDate}
          onChange={(e) => onDateChange(e.target.value)}
        >
          <option value="all">Alla datum</option>
          {availableDates.map((date) => (
            <option key={date} value={date}>{formatDate(date)}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}