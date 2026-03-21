-- 003_seed_data.sql
-- Exact port of DbQuery.SeedDataIfEmpty content.
-- WARNING: This script resets seeded domain data to match DbQuery exactly.

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE Booked_Seats;
TRUNCATE TABLE Bookings;
TRUNCATE TABLE Showings;
TRUNCATE TABLE Reviews;
TRUNCATE TABLE Actors;
TRUNCATE TABLE Directors;
TRUNCATE TABLE Films;
TRUNCATE TABLE Seats;
TRUNCATE TABLE Salongs;
TRUNCATE TABLE Ticket_Type;
TRUNCATE TABLE Contacts;
TRUNCATE TABLE acl;
TRUNCATE TABLE Users;
TRUNCATE TABLE sessions;
SET FOREIGN_KEY_CHECKS = 1;

-- ACL rules from DbQuery seed
INSERT INTO acl (userRoles, method, allow, route, `match`, comment) VALUES
            ('visitor,user,admin', '*',    'allow', '/api/login', 'true', 'Allow login/session routes'),
            ('visitor,user,admin', 'POST', 'allow', '/api/users', 'true', 'Allow registration for all roles'),
            ('visitor,user,admin', 'POST', 'allow', '/api/chat', 'true', 'Allow AI chat for all'),

            ('visitor,user,admin', 'GET',  'allow', '/api/films', 'true', 'Allow all to read films'),
            ('visitor,user,admin', 'GET',  'allow', '/api/films/{id}', 'true', 'Allow all to read single film'),

            ('visitor,user,admin', 'GET',  'allow', '/api/directors', 'true', 'Allow all to read directors'),
            ('visitor,user,admin', 'GET',  'allow', '/api/directors/{id}', 'true', 'Allow all to read single director'),

            ('visitor,user,admin', 'GET',  'allow', '/api/actors', 'true', 'Allow all to read actors'),
            ('visitor,user,admin', 'GET',  'allow', '/api/actors/{id}', 'true', 'Allow all to read single actor'),

            ('visitor,user,admin', 'GET',  'allow', '/api/showings', 'true', 'Allow all to read showings'),
            ('visitor,user,admin', 'GET',  'allow', '/api/showings/{id}', 'true', 'Allow all to read single showing'),

            ('visitor,user,admin', 'GET',  'allow', '/api/seats', 'true', 'Allow all to read seats'),
            ('visitor,user,admin', 'GET',  'allow', '/api/seats/{id}', 'true', 'Allow all to read single seat'),

            ('visitor,user,admin', 'GET',  'allow', '/api/booking_details', 'true', 'Allow all to read booking details'),
            ('visitor,user,admin', 'GET',  'allow', '/api/booking_details/{id}', 'true', 'Allow all to read single booking detail'),

            ('visitor,user,admin', 'GET',  'allow', '/api/showings_detail', 'true', 'Allow all to read showing details'),
            ('visitor,user,admin', 'GET',  'allow', '/api/showings_detail/{id}', 'true', 'Allow all to read single showing detail'),

            ('visitor,user,admin', 'GET',  'allow', '/api/ticket_type', 'true', 'Allow all to read ticket types'),
            ('visitor,user,admin', 'GET',  'allow', '/api/ticket_type/{id}', 'true', 'Allow all to read single ticket type'),

            ('visitor,user,admin', 'GET',  'allow', '/api/showings/{showingId}/seats/stream', 'true', 'Allow all to receive seat availability stream'),
            ('visitor,user,admin', 'POST', 'allow', '/api/showings/{showingId}/seats/lock', 'true', 'Allow all to lock seats during booking'),
            ('visitor,user,admin', 'POST', 'allow', '/api/showings/{showingId}/seats/release', 'true', 'Allow all to release seat locks during booking'),

            ('visitor,user,admin', 'POST', 'allow', '/api/bookings', 'true', 'Allow anyone to create bookings'),
            ('user,admin',         'GET',  'allow', '/api/bookings', 'true', 'Allow logged-in users to view bookings where backend permits'),
            ('user,admin',         'GET',  'allow', '/api/bookings/{id}', 'true', 'Allow logged-in users to view booking detail'),
            ('user,admin',         'DELETE', 'allow', '/api/bookings/{id}', 'true', 'Allow logged-in users to cancel booking'),

            ('user,admin',         'GET',  'allow', '/api/users/{id}', 'true', 'Allow profile read'),
            ('user,admin',         'PUT',  'allow', '/api/users/{id}', 'true', 'Allow profile update'),

            ('visitor,user,admin', 'POST', 'allow', '/api/contacts', 'true', 'Allow all to send contact messages'),
            ('visitor,user,admin', 'GET',  'allow', '/api/debug/showings', 'true', 'Allow debug showings during development'),

            ('admin',              '*',    'allow', '/api/acl', 'true', 'Allow admins to manage ACL'),
            ('admin',              '*',    'allow', '/api/sessions', 'true', 'Allow admins to manage sessions'),

            ('admin',              'GET',  'allow', '/api/contacts', 'true', 'Allow admin to list contact messages'),
            ('admin',              'GET',  'allow', '/api/contacts/{id}', 'true', 'Allow admin to read single contact message'),
            ('admin',              'PUT',  'allow', '/api/contacts/{id}', 'true', 'Allow admin to update contact status'),

            ('admin',               '*',   'allow', '/api/admin/*',       'true', 'Admin full access to admin routes');
-- Admin user from DbQuery seed (password: admin123, hash generated by BCrypt.Net-Next EnhancedHashPassword)
INSERT INTO Users (email, firstName, lastName, role, password)
VALUES ('admin@cinemahub.com', 'Admin', 'User', 'admin', '$2a$13$IcJanqJPXNIuqYe5ENBL1uUjOnjpc3H845o2YpSoS3JQAS00zSJ0W');

-- Domain data from DbQuery seed
-- Ticket_Type
                INSERT INTO Ticket_Type (name, price) VALUES
                ('Vuxen', 140.00),
                ('Pensionär', 120.00),
                ('Barn', 80.00);

                -- Salongs
                INSERT INTO Salongs (name) VALUES
                ('Stora Salongen'),
                ('Lilla Salongen');

                -- Seats för Stora Salongen (id 1): 8 rader [8,9,10,10,10,10,12,12] = 81 platser
                INSERT INTO Seats (salong_id, row_num, seat_number) VALUES
                (1,1,1),(1,1,2),(1,1,3),(1,1,4),(1,1,5),(1,1,6),(1,1,7),(1,1,8),
                (1,2,1),(1,2,2),(1,2,3),(1,2,4),(1,2,5),(1,2,6),(1,2,7),(1,2,8),(1,2,9),
                (1,3,1),(1,3,2),(1,3,3),(1,3,4),(1,3,5),(1,3,6),(1,3,7),(1,3,8),(1,3,9),(1,3,10),
                (1,4,1),(1,4,2),(1,4,3),(1,4,4),(1,4,5),(1,4,6),(1,4,7),(1,4,8),(1,4,9),(1,4,10),
                (1,5,1),(1,5,2),(1,5,3),(1,5,4),(1,5,5),(1,5,6),(1,5,7),(1,5,8),(1,5,9),(1,5,10),
                (1,6,1),(1,6,2),(1,6,3),(1,6,4),(1,6,5),(1,6,6),(1,6,7),(1,6,8),(1,6,9),(1,6,10),
                (1,7,1),(1,7,2),(1,7,3),(1,7,4),(1,7,5),(1,7,6),(1,7,7),(1,7,8),(1,7,9),(1,7,10),(1,7,11),(1,7,12),
                (1,8,1),(1,8,2),(1,8,3),(1,8,4),(1,8,5),(1,8,6),(1,8,7),(1,8,8),(1,8,9),(1,8,10),(1,8,11),(1,8,12);

                -- Seats för Lilla Salongen (id 2): 6 rader [6,8,9,10,10,12] = 55 platser
                INSERT INTO Seats (salong_id, row_num, seat_number) VALUES
                (2,1,1),(2,1,2),(2,1,3),(2,1,4),(2,1,5),(2,1,6),
                (2,2,1),(2,2,2),(2,2,3),(2,2,4),(2,2,5),(2,2,6),(2,2,7),(2,2,8),
                (2,3,1),(2,3,2),(2,3,3),(2,3,4),(2,3,5),(2,3,6),(2,3,7),(2,3,8),(2,3,9),
                (2,4,1),(2,4,2),(2,4,3),(2,4,4),(2,4,5),(2,4,6),(2,4,7),(2,4,8),(2,4,9),(2,4,10),
                (2,5,1),(2,5,2),(2,5,3),(2,5,4),(2,5,5),(2,5,6),(2,5,7),(2,5,8),(2,5,9),(2,5,10),
                (2,6,1),(2,6,2),(2,6,3),(2,6,4),(2,6,5),(2,6,6),(2,6,7),(2,6,8),(2,6,9),(2,6,10),(2,6,11),(2,6,12);

                -- Films
                INSERT INTO Films (title, description, duration_minutes, age_rating, genre, images, trailers) VALUES
                ('The Godfather', 'Den legendariska historien om Corleone-familjen och deras maffiaimperium.', 175, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BNGEwYjgwOGQtYjg5ZS00Njc1LTk2ZGEtM2QwZWQ2NjdhZTE5XkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=sY1S34973zA"]'),

                ('Goodfellas', 'En ung man växer upp i den italiensk-amerikanska maffian.', 146, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BN2E5NzI2ZGMtY2VjNi00YTRjLWI1MDUtZGY5OWU1MWJjZjRjXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=qo5jJpHtI1Y"]'),

                ('Scarface', 'Tony Montana bygger upp ett brutalt kokainimperium i Miami.', 170, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BNDUzYjY0NmUtMDM4OS00Y2Q5LWJiODYtNTk0ZTk0YjZhMTg1XkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=7pQQHnqBa2E"]'),

                ('Casino', 'Historien om maffians kontroll över Las Vegas.', 178, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BMDRlZWZjZjYtYzY2NS00ZWVjLTkwYzAtZTA2ZDAzMGRiYmYwXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=EJXDMwGWhoA"]'),

                ('The Departed', 'En polis infiltrerar maffian samtidigt som en gangster infiltrerar polisen.', 151, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BMTI1MTY2OTIxNV5BMl5BanBnXkFtZTYwNjQ4NjY3._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=iojhqm0JTW4"]'),

                ('American Gangster', 'Frank Lucas bygger ett heroinimperium i Harlem.', 157, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BZGExM2MwNjUtNThkNi00ZjBmLWJhZDgtN2ZmOWJiZWEwNGMxXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=BV_nssS6Zkg"]'),

                ('The Irishman', 'En hitman berättar historien om sitt liv inom maffian.', 209, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BMTY2YThkNmQtOWJhYy00ZDc3LWEzOGEtMmQwNzM0YjFmZWIyXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=WHXxVmeGQUc"]'),

                ('The Gentlemen', 'En brittisk cannabisbaron försöker sälja sitt imperium.', 113, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BMjE2ZjQ4ZGMtZjFhMi00NmI5LTliNjEtODczMWMxNjliZjgxXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=2B0RpUGss2c"]'),

                ('The Boss Baby', 'En bebis som egentligen är en hemlig agent försöker stoppa en ond plan.', 97, '0', 'Animerat',
                '["https://m.media-amazon.com/images/M/MV5BOWEwYWY5NWItMDQ0NS00MjkzLWE1MDEtNTQyYTQ3MmM1NTUyXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=tquIfapGVqs"]'),

                ('Zootopia', 'En polis-kanin och en småkriminell räv löser ett mysterium.', 108, '7', 'Animerat',
                '["https://m.media-amazon.com/images/M/MV5BOTMyMjEyNzIzMV5BMl5BanBnXkFtZTgwNzIyNjU0NzE@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=jWM0ct-OLsM"]'),

                ('Hotel Transylvania', 'Monster driver ett hotell där människor egentligen inte får komma in.', 91, '7', 'Familj',
                '["https://m.media-amazon.com/images/M/MV5BMTM3NjQyODI3M15BMl5BanBnXkFtZTcwMDM4NjM0OA@@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=q4RK3jY7AVk"]'),

                ('Beetlejuice', 'Ett spökpar försöker skrämma bort nya invånare från sitt hus.', 92, '11', 'Skräck',
                '["https://m.media-amazon.com/images/M/MV5BYTQxZTUzOTgtZmU4MC00NTc3LTkyMDMtYzNkYzAwMzc4NWQxXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=ickbVzajrk0"]'),

                ('Ghostbusters', 'Tre forskare startar en verksamhet som jagar spöken.', 105, '11', 'Komedi',
                '["https://m.media-amazon.com/images/M/MV5BMGI0Yjg2ODAtNDYzNi00Njc2LTlkMmMtMmRmYWI5MDE4ZGRkXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=vntAEVjPBzQ"]'),

                ('Donnie Brasco', 'En FBI-agent infiltrerar maffian och kommer farligt nära sitt mål.', 147, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BODBlNDIxZjgtYzI1Mi00MWNlLWI1NjYtNzY2M2IzYjgyOGFkXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=omIiE9KKj2o"]'),

                ('Public Enemies', 'Historien om bankrånaren John Dillinger under 1930-talet.', 140, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BMThiZTM5YTMtMzczMC00ZTRjLWE1NzQtMDA2MzViMmYwNjc3XkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=Ee92mDZu_PI"]'),

                ('Gangs of New York', 'Gängkrig bryter ut i New York under 1800-talet.', 167, '15', 'Krim',
                '["https://m.media-amazon.com/images/M/MV5BMmJmM2Q3MDYtMmFiNy00NTM2LWEzZGQtZDJmMGUyM2QwNTQxXkEyXkFqcGc@._V1_.jpg"]',
                '["https://www.youtube.com/watch?v=qHVUPri5tjA"]');


            -- Directors
                INSERT INTO Directors (film_id, name) VALUES
                (1, 'Francis Ford Coppola'),
                (2, 'Martin Scorsese'),
                (3, 'Brian De Palma'),
                (4, 'Martin Scorsese'),
                (5, 'Martin Scorsese'),
                (6, 'Ridley Scott'),
                (7, 'Martin Scorsese'),
                (8, 'Guy Ritchie'),
                (9, 'Tom McGrath'),
                (10, 'Byron Howard'),
                (11, 'Genndy Tartakovsky'),
                (12, 'Tim Burton'),
                (13, 'Ivan Reitman'),
                (14, 'Mike Newell'),
                (15, 'Michael Mann'),
                (16, 'Martin Scorsese');


                -- Actors
                INSERT INTO Actors (film_id, name, role_order) VALUES
                (1, 'Marlon Brando', 1),
                (1, 'Al Pacino', 2),
                (1, 'James Caan', 3),

                (2, 'Robert De Niro', 1),
                (2, 'Ray Liotta', 2),
                (2, 'Joe Pesci', 3),

                (3, 'Al Pacino', 1),
                (3, 'Michelle Pfeiffer', 2),
                (3, 'Steven Bauer', 3),

                (4, 'Robert De Niro', 1),
                (4, 'Sharon Stone', 2),
                (4, 'Joe Pesci', 3),

                (5, 'Leonardo DiCaprio', 1),
                (5, 'Matt Damon', 2),
                (5, 'Jack Nicholson', 3),

                (6, 'Denzel Washington', 1),
                (6, 'Russell Crowe', 2),

                (7, 'Robert De Niro', 1),
                (7, 'Al Pacino', 2),
                (7, 'Joe Pesci', 3),

                (8, 'Matthew McConaughey', 1),
                (8, 'Charlie Hunnam', 2),
                (8, 'Hugh Grant', 3),

                (9, 'Alec Baldwin', 1),
                (9, 'Steve Buscemi', 2),

                (10, 'Ginnifer Goodwin', 1),
                (10, 'Jason Bateman', 2),
                (10, 'Idris Elba', 3),

                (11, 'Adam Sandler', 1),
                (11, 'Andy Samberg', 2),

                (12, 'Michael Keaton', 1),
                (12, 'Winona Ryder', 2),
                (12, 'Alec Baldwin', 3),

                (13, 'Bill Murray', 1),
                (13, 'Dan Aykroyd', 2),
                (13, 'Sigourney Weaver', 3),

                (14, 'Johnny Depp', 1),
                (14, 'Al Pacino', 2),

                (15, 'Johnny Depp', 1),
                (15, 'Christian Bale', 2),

                (16, 'Leonardo DiCaprio', 1),
                (16, 'Daniel Day-Lewis', 2),
                (16, 'Cameron Diaz', 3);

                -- Reviews (några filmer med 2-3, några med 0)
                INSERT INTO Reviews (film_id, source, quote, stars, max_stars) VALUES
                (1, 'IMDb', 'En av filmhistoriens största klassiker.', 10, 10),
                (1, 'Empire', 'Ett mästerverk inom gangsterfilm.', 5, 5),

                (2, 'Rotten Tomatoes', 'Scorsese i toppform.', 5, 5),
                (2, 'Aftonbladet', 'Brutalt, realistiskt och fantastiskt skådespel.', 4, 5),

                (3, 'IMDb', 'Al Pacino är ikonisk som Tony Montana.', 8, 10),
                (3, 'Expressen', 'En rå och stilbildande gangsterfilm.', 4, 5),

                (4, 'Empire', 'Las Vegas, maffia och Scorsese – perfekt kombination.', 4, 5),
                (4, 'IMDb', 'En episk berättelse om makt och girighet.', 8, 10),

                (5, 'Rotten Tomatoes', 'Spännande från början till slut.', 5, 5),
                (5, 'SvD', 'Ett intelligent kriminaldrama.', 4, 5),

                (6, 'IMDb', 'Denzel Washington levererar en av sina bästa roller.', 8, 10),
                (6, 'Empire', 'Elegant och intensivt kriminaldrama.', 4, 5),

                (7, 'Aftonbladet', 'Scorseses stora avsked till gangsterfilmen.', 4, 5),
                (7, 'IMDb', 'Ett episkt och långsamt men briljant drama.', 8, 10),

                (8, 'Rotten Tomatoes', 'Smart, rolig och våldsam gangsterkomedi.', 4, 5),
                (8, 'IMDb', 'Guy Ritchie i toppform.', 7, 10),

                (9, 'IMDb', 'En charmig och rolig familjefilm.', 7, 10),
                (9, 'Expressen', 'Barnfilm med humor även för vuxna.', 3, 5),

                (10, 'Rotten Tomatoes', 'Smart, rolig och visuellt imponerande.', 5, 5),
                (10, 'SvD', 'En av Disneys bästa moderna filmer.', 4, 5),

                (11, 'IMDb', 'Rolig familjefilm med monsterhumor.', 7, 10),
                (11, 'Aftonbladet', 'Charmig animation för hela familjen.', 3, 5),

                (12, 'Empire', 'Tim Burton på sitt mest kreativa.', 4, 5),
                (12, 'IMDb', 'En kultklassiker fylld med galen humor.', 8, 10),

                (13, 'IMDb', 'En klassisk komedi som fortfarande håller.', 8, 10),
                (13, 'Expressen', 'Spökjägarna är fortfarande lika roliga.', 3, 5),

                (14, 'Rotten Tomatoes', 'Johnny Depp och Pacino i en spännande historia.', 4, 5),
                (14, 'IMDb', 'En av de bästa maffia-infiltrationsfilmerna.', 8, 10),

                (15, 'Empire', 'Elegant gangsterfilm om John Dillinger.', 4, 5),
                (15, 'IMDb', 'Stilren och intensiv kriminalhistoria.', 7, 10),

                (16, 'SvD', 'Ett episkt drama om New Yorks gängkrig.', 4, 5),
                (16, 'IMDb', 'Scorsese levererar ännu ett historiskt epos.', 7, 10);


                -- Showings (relative to current date, richer seed for AI testing)
                INSERT INTO Showings (film_id, salong_id, start_time, language, subtitle) VALUES

                (1,1,DATE_ADD(CURDATE(), INTERVAL 0 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (1,2,DATE_ADD(CURDATE(), INTERVAL 1 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (1,1,DATE_ADD(CURDATE(), INTERVAL 2 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (2,2,DATE_ADD(CURDATE(), INTERVAL 1 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (2,1,DATE_ADD(CURDATE(), INTERVAL 2 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (2,2,DATE_ADD(CURDATE(), INTERVAL 3 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (3,1,DATE_ADD(CURDATE(), INTERVAL 2 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (3,2,DATE_ADD(CURDATE(), INTERVAL 3 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (3,1,DATE_ADD(CURDATE(), INTERVAL 4 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (4,2,DATE_ADD(CURDATE(), INTERVAL 3 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (4,1,DATE_ADD(CURDATE(), INTERVAL 4 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (4,2,DATE_ADD(CURDATE(), INTERVAL 5 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (5,1,DATE_ADD(CURDATE(), INTERVAL 4 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (5,2,DATE_ADD(CURDATE(), INTERVAL 5 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (5,1,DATE_ADD(CURDATE(), INTERVAL 6 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (6,2,DATE_ADD(CURDATE(), INTERVAL 5 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (6,1,DATE_ADD(CURDATE(), INTERVAL 6 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (6,2,DATE_ADD(CURDATE(), INTERVAL 7 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (7,1,DATE_ADD(CURDATE(), INTERVAL 6 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (7,2,DATE_ADD(CURDATE(), INTERVAL 7 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (7,1,DATE_ADD(CURDATE(), INTERVAL 8 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (8,2,DATE_ADD(CURDATE(), INTERVAL 7 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (8,1,DATE_ADD(CURDATE(), INTERVAL 8 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (8,2,DATE_ADD(CURDATE(), INTERVAL 9 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (9,1,DATE_ADD(CURDATE(), INTERVAL 8 DAY) + INTERVAL 14 HOUR,'Engelska','Svenska'),
                (9,2,DATE_ADD(CURDATE(), INTERVAL 9 DAY) + INTERVAL 15 HOUR,'Engelska','Svenska'),
                (9,1,DATE_ADD(CURDATE(), INTERVAL 10 DAY) + INTERVAL 16 HOUR,'Engelska','Svenska'),

                (10,2,DATE_ADD(CURDATE(), INTERVAL 9 DAY) + INTERVAL 14 HOUR,'Engelska','Svenska'),
                (10,1,DATE_ADD(CURDATE(), INTERVAL 10 DAY) + INTERVAL 15 HOUR,'Engelska','Svenska'),
                (10,2,DATE_ADD(CURDATE(), INTERVAL 11 DAY) + INTERVAL 16 HOUR,'Engelska','Svenska'),

                (11,1,DATE_ADD(CURDATE(), INTERVAL 10 DAY) + INTERVAL 13 HOUR,'Engelska','Svenska'),
                (11,2,DATE_ADD(CURDATE(), INTERVAL 11 DAY) + INTERVAL 14 HOUR,'Engelska','Svenska'),
                (11,1,DATE_ADD(CURDATE(), INTERVAL 12 DAY) + INTERVAL 15 HOUR,'Engelska','Svenska'),

                (12,2,DATE_ADD(CURDATE(), INTERVAL 11 DAY) + INTERVAL 16 HOUR,'Engelska','Svenska'),
                (12,1,DATE_ADD(CURDATE(), INTERVAL 12 DAY) + INTERVAL 17 HOUR,'Engelska','Svenska'),
                (12,2,DATE_ADD(CURDATE(), INTERVAL 13 DAY) + INTERVAL 18 HOUR,'Engelska','Svenska'),

                (13,1,DATE_ADD(CURDATE(), INTERVAL 12 DAY) + INTERVAL 17 HOUR,'Engelska','Svenska'),
                (13,2,DATE_ADD(CURDATE(), INTERVAL 13 DAY) + INTERVAL 18 HOUR,'Engelska','Svenska'),
                (13,1,DATE_ADD(CURDATE(), INTERVAL 14 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),

                (14,2,DATE_ADD(CURDATE(), INTERVAL 13 DAY) + INTERVAL 18 HOUR,'Engelska','Svenska'),
                (14,1,DATE_ADD(CURDATE(), INTERVAL 14 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (14,2,DATE_ADD(CURDATE(), INTERVAL 15 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),

                (15,1,DATE_ADD(CURDATE(), INTERVAL 14 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (15,2,DATE_ADD(CURDATE(), INTERVAL 15 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (15,1,DATE_ADD(CURDATE(), INTERVAL 16 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska'),

                (16,2,DATE_ADD(CURDATE(), INTERVAL 15 DAY) + INTERVAL 19 HOUR,'Engelska','Svenska'),
                (16,1,DATE_ADD(CURDATE(), INTERVAL 16 DAY) + INTERVAL 20 HOUR,'Engelska','Svenska'),
                (16,2,DATE_ADD(CURDATE(), INTERVAL 17 DAY) + INTERVAL 21 HOUR,'Engelska','Svenska');

                -- Bookings (nästan full showing 1 = Inception i Stora Salongen)
                -- Lediga: rad3 p5-7 (id 22,23,24), rad4 p5,6,8 (id 32,33,35),
                --         rad7 p10-12 (id 67,68,69), rad8 p10-12 (id 79,80,81)
                INSERT INTO Bookings (email, showing_id, booking_reference) VALUES
                ('anna.svensson@email.se', 1, 'AB34CD'),
                ('erik.johansson@email.se', 1, 'XY7K3M'),
                ('lisa.andersson@email.se', 1, 'P9R2HW');

                INSERT INTO Booked_Seats (seat_id, showing_id, booking_id, ticket_type_id) VALUES
                -- Rad 1 (id 1-8): alla bokade
                (1,1,1,1),(2,1,1,1),(3,1,1,2),(4,1,1,1),(5,1,1,3),(6,1,1,1),(7,1,1,1),(8,1,1,2),
                -- Rad 2 (id 9-17): alla bokade
                (9,1,1,1),(10,1,1,1),(11,1,1,3),(12,1,1,1),(13,1,1,2),(14,1,1,1),(15,1,1,1),(16,1,1,1),(17,1,1,3),
                -- Rad 3 (id 18-27): plats 1-4 och 8-10 bokade, plats 5-7 (id 22,23,24) lediga
                (18,1,2,1),(19,1,2,1),(20,1,2,2),(21,1,2,1),
                (25,1,2,1),(26,1,2,3),(27,1,2,1),
                -- Rad 4 (id 28-37): plats 1-4,7,9,10 bokade, plats 5,6,8 (id 32,33,35) lediga
                (28,1,2,1),(29,1,2,2),(30,1,2,1),(31,1,2,1),
                (34,1,2,1),(36,1,2,3),(37,1,2,1),
                -- Rad 5 (id 38-47): alla bokade
                (38,1,2,1),(39,1,2,1),(40,1,2,2),(41,1,2,1),(42,1,2,3),(43,1,2,1),(44,1,2,1),(45,1,2,1),(46,1,2,2),(47,1,2,1),
                -- Rad 6 (id 48-57): alla bokade
                (48,1,3,1),(49,1,3,1),(50,1,3,3),(51,1,3,1),(52,1,3,2),(53,1,3,1),(54,1,3,1),(55,1,3,1),(56,1,3,2),(57,1,3,1),
                -- Rad 7 (id 58-69): plats 1-9 bokade, plats 10-12 (id 67,68,69) lediga
                (58,1,3,1),(59,1,3,2),(60,1,3,1),(61,1,3,1),(62,1,3,3),(63,1,3,1),(64,1,3,1),(65,1,3,1),(66,1,3,2),
                -- Rad 8 (id 70-81): plats 1-9 bokade, plats 10-12 (id 79,80,81) lediga
                (70,1,3,1),(71,1,3,1),(72,1,3,3),(73,1,3,1),(74,1,3,2),(75,1,3,1),(76,1,3,1),(77,1,3,1),(78,1,3,2);
