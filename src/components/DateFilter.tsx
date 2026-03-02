type DateFilterProps = {
  selectedDate: string;
  onDateChange: (date: string) => void;
  availableDates?: string[];
};

export default function DateFilter({
  selectedDate,
  onDateChange,
  availableDates = [],
}: DateFilterProps) {

  const formatDateFull = (dateStr: string) => {
    const date = new Date(dateStr);
    const weekday = date.toLocaleDateString("sv-SE", { weekday: "long" });
    const day = date.getDate();
    const month = date.toLocaleDateString("sv-SE", { weekday: "long" });
    return `${weekday.charAt(0).toUpperCase() + weekday.slice(1)} ${day} ${month}`;
  };

  return (
    <div className="mb-3">
      <label htmlFor="dateFilter" className="form-label small text-muted">Datum</label>
      <select
        id="dateFilter"
        className="form-select w-auto"
        value={selectedDate}
        onChange={(e) => onDateChange(e.target.value)}
      >
        <option value="all">Alla datum</option>
        {availableDates.map((date) => (
          <option key={date} value={date}>
            {formatDateFull(date)}
          </option>
        ))}
      </select>
    </div>
  );
}