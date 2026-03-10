import { useState } from "react";

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

  const [showDatePicker, setShowDatePicker] = useState(false);

  const formatDateFull = (dateString: string) => {
    const date = new Date(dateString);
    const weekday = date.toLocaleDateString("sv-SE", { weekday: "long" });
    const day = date.getDate();
    const month = date.toLocaleDateString("sv-SE", { month: "long" });
    return `${weekday.charAt(0).toUpperCase() + weekday.slice(1)} ${day} ${month}`;
  };

  const nextSevenDays = availableDates.slice(0, 7);
  const hasMoreDates = availableDates.length > 7;

  return (
    <div className="mb-3">
      <label htmlFor="dateFilter" className="form-label small text-muted">Datum</label>

      {!showDatePicker ? (
        <>
          <select
            id="dateFilter"
            className="form-select w-auto"
            value={selectedDate}
            onChange={(e) => onDateChange(e.target.value)}
          >
            <option value="all">Alla datum</option>
            {nextSevenDays.map((date) => (
              <option key={date} value={date}>
                {formatDateFull(date)}
              </option>
            ))}
          </select>

          {hasMoreDates && (
            <button
              type="button"
              className="btn btn-link btn-sm p-0 mt-2"
              onClick={() => setShowDatePicker(true)}
            >
              Visa fler datum →
            </button>
          )}
        </>
      ) : (
        <>
          <input
            type="date"
            className="form-control w-auto"
            value={selectedDate !== "all" ? selectedDate : ""}
            onChange={(e) => {
              if (e.target.value) {
                onDateChange(e.target.value);
              }
            }}
            min={availableDates[0]}
            max={availableDates[availableDates.length - 1]}
          />

          <button
            type="button"
            className="btn btn-link btn-sm p-0 mt-2"
            onClick={() => {
              setShowDatePicker(false);
              onDateChange("all");
            }}
          >
            ← Tillbaka
          </button>
        </>
      )}
    </div>
  );
}