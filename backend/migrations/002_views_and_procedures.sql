-- 002_views_and_procedures.sql
-- Recreates stored procedures and views. Safe to run multiple times.

DROP PROCEDURE IF EXISTS CreateBookingWithSeats;
DROP PROCEDURE IF EXISTS DeleteBooking;
DROP VIEW IF EXISTS showings_detail;
DROP VIEW IF EXISTS booking_details;

-- Views
CREATE VIEW showings_detail AS
SELECT
    s.id,
    s.film_id,
    s.salong_id,
    s.start_time,
    s.language,
    s.subtitle,
    f.title AS film_title,
    f.description AS film_description,
    f.duration_minutes,
    f.age_rating,
    f.genre,
    f.images AS film_images,
    f.trailers AS film_trailers,
    sa.name AS salong_name
FROM Showings s
JOIN Films f ON s.film_id = f.id
JOIN Salongs sa ON s.salong_id = sa.id;

CREATE VIEW booking_details AS
SELECT
    b.id AS booking_id,
    b.booking_reference,
    b.email,
    b.booked_at,
    s.start_time,
    f.title AS film_title,
    f.description AS film_description,
    f.duration_minutes,
    f.age_rating,
    f.genre,
    f.images AS film_images,
    sa.name AS salong_name,
    JSON_ARRAYAGG(
        JSON_OBJECT(
            'row_num', se.row_num,
            'seat_number', se.seat_number,
            'ticket_type', tt.name,
            'ticket_price', tt.price
        )
    ) AS seats
FROM Bookings b
JOIN Showings s ON b.showing_id = s.id
JOIN Films f ON s.film_id = f.id
JOIN Salongs sa ON s.salong_id = sa.id
JOIN Booked_Seats bs ON bs.booking_id = b.id
JOIN Seats se ON bs.seat_id = se.id
JOIN Ticket_Type tt ON bs.ticket_type_id = tt.id
GROUP BY b.id, b.booking_reference, b.email, b.booked_at,
         s.start_time, f.title, f.description, f.duration_minutes,
         f.age_rating, f.genre, f.images, sa.name;

-- Stored Procedures
DELIMITER //

CREATE PROCEDURE CreateBookingWithSeats(
    IN customer_email VARCHAR(255),
    IN selected_showing_id INT,
    IN selected_seats_json JSON,
    IN booking_ref VARCHAR(10)
)
BEGIN
    DECLARE v_booking_id INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    INSERT INTO Bookings (email, showing_id, booking_reference) VALUES (customer_email, selected_showing_id, booking_ref);
    SET v_booking_id = LAST_INSERT_ID();

    INSERT INTO Booked_Seats (seat_id, showing_id, booking_id, ticket_type_id)
    SELECT seats.seat_id, selected_showing_id, v_booking_id, seats.ticket_type_id
    FROM JSON_TABLE(selected_seats_json, '$[*]' COLUMNS(
        seat_id INT PATH '$.seat_id',
        ticket_type_id INT PATH '$.ticket_type_id'
    )) AS seats;

    COMMIT;

    SELECT v_booking_id AS bookingId, booking_ref AS booking_reference;
END //

CREATE PROCEDURE DeleteBooking(
    IN booking_ref_param VARCHAR(10)
)
BEGIN
    DECLARE v_booking_id INT;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
    END;

    SELECT id INTO v_booking_id FROM Bookings WHERE booking_reference = booking_ref_param;

    START TRANSACTION;

    DELETE FROM Booked_Seats
    WHERE booking_id = v_booking_id;

    DELETE FROM Bookings
    WHERE id = v_booking_id;

    COMMIT;
END //

DELIMITER ;





;

;
