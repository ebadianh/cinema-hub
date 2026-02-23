import { useState, useEffect, useCallback } from "react";
import type { Seat, TicketType, SelectedSeat } from "../interfaces/Booking";
import { calculateTotalTickets, groupSeatsByRow } from "../utils/bookingUtils";

export default function useSeatSelection(
  seats: Seat[],
  ticketTypes: TicketType[],
  unavailableSeatIds: Set<number>,
  lockedByMe: Set<number>,
  defaultTicketCounts: Record<number, number>
) {
  const [ticketCounts, setTicketCounts] = useState<Record<number, number>>({});
  const [selectedSeats, setSelectedSeats] = useState<SelectedSeat[]>([]);
  const [previewSeatIds, setPreviewSeatIds] = useState<Set<number>>(new Set());
  const [manualMode, setManualMode] = useState(false);

  // Apply defaults once
  useEffect(() => {
    if (Object.keys(defaultTicketCounts).length === 0) return;
    setTicketCounts(prev => {
      const totalExisting = calculateTotalTickets(prev);
      if (totalExisting > 0) return prev;
      return { ...prev, ...defaultTicketCounts };
    });
  }, [defaultTicketCounts]);

  const totalTickets = calculateTotalTickets(ticketCounts);

  const assignTicketTypes = useCallback(
    (seatList: Seat[]): SelectedSeat[] => {
      const result: SelectedSeat[] = [];
      let seatIndex = 0;
      for (const type of ticketTypes) {
        const count = ticketCounts[type.id] || 0;
        for (let i = 0; i < count && seatIndex < seatList.length; i++) {
          result.push({ seat: seatList[seatIndex], ticketType: type });
          seatIndex++;
        }
      }
      return result;
    },
    [ticketTypes, ticketCounts]
  );

  const allocateFromSeat = useCallback(
    (clicked: Seat): Seat[] => {
      const seatsByRow = groupSeatsByRow(seats);
      const rows = Object.keys(seatsByRow).map(Number).sort((a, b) => a - b);

      const seatLookup = new Map<string, Seat>();
      for (const r of rows) {
        for (const s of seatsByRow[r] || []) {
          if (!unavailableSeatIds.has(s.id)) {
            seatLookup.set(`${r}-${s.seat_number}`, s);
          }
        }
      }

      const clickedIdx = rows.indexOf(clicked.row_num);
      const spiralRows: number[] = [clicked.row_num];
      for (let ring = 1; ring < rows.length; ring++) {
        if (clickedIdx + ring < rows.length) spiralRows.push(rows[clickedIdx + ring]);
        if (clickedIdx - ring >= 0) spiralRows.push(rows[clickedIdx - ring]);
      }

      const maxSeatNum = Math.max(...seats.map(s => s.seat_number));
      const maxDist = Math.max(maxSeatNum, rows.length);
      const collected: Seat[] = [];
      const collectedIds = new Set<number>();
      let needed = totalTickets;

      for (let d = 0; d <= maxDist && needed > 0; d++) {
        for (const r of spiralRows) {
          if (needed <= 0) break;
          const rowDist = Math.abs(r - clicked.row_num);
          if (rowDist > d) continue;

          if (rowDist === d) {
            for (let sd = 0; sd <= d && needed > 0; sd++) {
              const positions =
                sd === 0
                  ? [clicked.seat_number]
                  : [clicked.seat_number + sd, clicked.seat_number - sd];
              for (const pos of positions) {
                if (needed <= 0) break;
                if (pos < 1 || pos > maxSeatNum) continue;
                const seat = seatLookup.get(`${r}-${pos}`);
                if (seat && !collectedIds.has(seat.id)) {
                  collected.push(seat);
                  collectedIds.add(seat.id);
                  needed--;
                }
              }
            }
          } else {
            const positions = [clicked.seat_number + d, clicked.seat_number - d];
            for (const pos of positions) {
              if (needed <= 0) break;
              if (pos < 1 || pos > maxSeatNum) continue;
              const seat = seatLookup.get(`${r}-${pos}`);
              if (seat && !collectedIds.has(seat.id)) {
                collected.push(seat);
                collectedIds.add(seat.id);
                needed--;
              }
            }
          }
        }
      }

      return collected;
    },
    [seats, unavailableSeatIds, totalTickets]
  );

  const handleCountChange = useCallback((ticketTypeId: number, count: number) => {
    setTicketCounts(prev => ({ ...prev, [ticketTypeId]: count }));
    setSelectedSeats([]);
  }, []);

  const handleSeatClick = useCallback(
    (clicked: Seat) => {
      if (totalTickets === 0) return;

      if (manualMode) {
        const alreadySelected = selectedSeats.find(s => s.seat.id === clicked.id);
        if (alreadySelected) {
          const remaining = selectedSeats.filter(s => s.seat.id !== clicked.id).map(s => s.seat);
          setSelectedSeats(assignTicketTypes(remaining));
        } else if (selectedSeats.length < totalTickets) {
          const newSeatList = [...selectedSeats.map(s => s.seat), clicked];
          setSelectedSeats(assignTicketTypes(newSeatList));
        }
      } else {
        const allocated = allocateFromSeat(clicked);
        setSelectedSeats(assignTicketTypes(allocated));
      }
      setPreviewSeatIds(new Set());
    },
    [totalTickets, manualMode, selectedSeats, assignTicketTypes, allocateFromSeat]
  );

  const handleSeatHover = useCallback(
    (seat: Seat) => {
      if (totalTickets === 0) return;
      if (unavailableSeatIds.has(seat.id)) return;

      if (manualMode) {
        const alreadySelected = selectedSeats.some(s => s.seat.id === seat.id);
        if (!alreadySelected && selectedSeats.length < totalTickets) {
          setPreviewSeatIds(new Set([seat.id]));
        }
      } else {
        const allocated = allocateFromSeat(seat);
        setPreviewSeatIds(new Set(allocated.map(s => s.id)));
      }
    },
    [totalTickets, unavailableSeatIds, manualMode, selectedSeats, allocateFromSeat]
  );

  const handleSeatLeave = useCallback(() => {
    setPreviewSeatIds(new Set());
  }, []);

  // Remove selected seats that become unavailable
  useEffect(() => {
    setSelectedSeats(prev => {
      const filtered = prev.filter(
        s => !unavailableSeatIds.has(s.seat.id) || lockedByMe.has(s.seat.id)
      );
      return filtered.length !== prev.length ? filtered : prev;
    });
  }, [unavailableSeatIds, lockedByMe]);

  return {
    ticketCounts,
    selectedSeats,
    previewSeatIds,
    manualMode,
    totalTickets,
    setManualMode,
    handleCountChange,
    handleSeatClick,
    handleSeatHover,
    handleSeatLeave,
  };
}
