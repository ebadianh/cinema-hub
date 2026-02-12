import { useState } from "react";


export default function SelectSeats() {
  const [adultTicket, setAdultTicket] = useState(2);
  const [childTicket, setChildTicket] = useState(0);
  const [seniorTicket, setSeniorTicket] = useState(0);

  const [date, setDate] = useState<Date>();
  const [time, setTime] = useState(null);

  const [error, setError] = useState<string | null>(null);

}