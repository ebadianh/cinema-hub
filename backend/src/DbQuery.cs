namespace WebApp;

public static class DbQuery
{

    // Setup the database connection from config
    private static string connectionString;

    // JSON columns for _CONTAINS_ validation (template had categories)
    // Your cinema schema doesn't use JSON columns right now:
    public static Arr JsonColumns = Arr(new string[] { });

    public static bool IsJsonColumn(string column) => JsonColumns.Includes(column);

    static DbQuery()
    {
        var configPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "db-config.json"
        );
        var configJson = File.ReadAllText(configPath);
        var config = JSON.Parse(configJson);

        connectionString =
            $"Server={config.host};Port={config.port};Database={config.database};" +
            $"User={config.username};Password={config.password};";

        var db = new MySqlConnection(connectionString);
        db.Open();

        // Create tables if they don't exist
        if (config.createTablesIfNotExist == true)
        {
            // drop db
            DropTablesIfExist(db);
            CreateTablesIfNotExist(db);
        }

        // Seed data if tables are empty
        if (config.seedDataIfEmpty == true)
        {
            SeedDataIfEmpty(db);

            // Create stored procedures
            CreateStoredProcedures(db);

            // Create views
            CreateViews(db);
        }

        db.Close();
    }

    public static void DropTablesIfExist(MySqlConnection db)
    {
        var dropTablesSQL = @"
        USE cinema_hub;
        -- Drop tables if they exist (in reverse dependency order)
        SET FOREIGN_KEY_CHECKS = 0;
        DROP TABLE IF EXISTS Booked_Seats;
        DROP TABLE IF EXISTS Bookings;
        DROP TABLE IF EXISTS Showings;
        DROP TABLE IF EXISTS Seats;
        DROP TABLE IF EXISTS Salongs;
        DROP TABLE IF EXISTS Films;
        DROP TABLE IF EXISTS Users;
        DROP TABLE IF EXISTS Ticket_Type;
        DROP TABLE IF EXISTS Directors;
        DROP TABLE IF EXISTS Actors;
        DROP TABLE IF EXISTS Reviews;
        DROP TABLE IF EXISTS Contacts;
        SET FOREIGN_KEY_CHECKS = 1; 
        ";

        // Execute each statement separately
        foreach (var sql in dropTablesSQL.Split(';'))
        {
            var trimmed = sql.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                var command = db.CreateCommand();
                command.CommandText = trimmed;
                command.ExecuteNonQuery();
            }
        }
    }

    private static void CreateTablesIfNotExist(MySqlConnection db)
    {
        // Keep sessions + acl (template uses them), remove products,
        // and create your cinema schema (plural).
        var createTablesSql = @"
            CREATE TABLE IF NOT EXISTS sessions (
                id VARCHAR(255) PRIMARY KEY NOT NULL,
                created DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                modified DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
                data JSON
            );

            CREATE TABLE IF NOT EXISTS acl (
                id INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
                userRoles VARCHAR(255) NOT NULL,
                method VARCHAR(50) NOT NULL DEFAULT 'GET',
                allow ENUM('allow', 'disallow') NOT NULL DEFAULT 'allow',
                route VARCHAR(255) NOT NULL,
                `match` ENUM('true', 'false') NOT NULL DEFAULT 'true',
                comment VARCHAR(500) NOT NULL DEFAULT '',
                UNIQUE KEY unique_acl (userRoles, method, route)
            );

            -- Users
            CREATE TABLE IF NOT EXISTS Users (
                id INT PRIMARY KEY AUTO_INCREMENT,
                created DATETIME DEFAULT CURRENT_TIMESTAMP,
                email VARCHAR(255) NOT NULL UNIQUE,
                firstName VARCHAR(100) NOT NULL,
                lastName VARCHAR(100) NOT NULL,
                role VARCHAR(50) NOT NULL DEFAULT 'user',
                password VARCHAR(255) NOT NULL
            );

            -- Films
            CREATE TABLE IF NOT EXISTS Films (
                id INT PRIMARY KEY AUTO_INCREMENT,
                title VARCHAR(255) NOT NULL,
                description TEXT,
                duration_minutes INT NOT NULL,
                age_rating VARCHAR(10),
                genre VARCHAR(100),
                images JSON,
                trailers JSON
            );

            -- Directors
            CREATE TABLE IF NOT EXISTS Directors (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                name VARCHAR(255) NOT NULL,
                FOREIGN KEY (film_id) REFERENCES Films(id) ON DELETE CASCADE
            );

            -- Actors
            CREATE TABLE IF NOT EXISTS Actors (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                name VARCHAR(255) NOT NULL,
                role_order INT NULL,
                FOREIGN KEY (film_id) REFERENCES Films(id) ON DELETE CASCADE
            );

            -- Reviews
            CREATE TABLE IF NOT EXISTS Reviews (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                source VARCHAR(100) NOT NULL,
                quote VARCHAR(255) NOT NULL,
                stars INT NOT NULL,
                max_stars INT NOT NULL,
                FOREIGN KEY (film_id) REFERENCES Films(id) ON DELETE CASCADE
            );

            -- Salongs
            CREATE TABLE IF NOT EXISTS Salongs (
                id INT PRIMARY KEY AUTO_INCREMENT,
                name VARCHAR(100) NOT NULL
            );

            -- Seats
            CREATE TABLE IF NOT EXISTS Seats (
                id INT PRIMARY KEY AUTO_INCREMENT,
                salong_id INT NOT NULL,
                row_num INT NOT NULL,
                seat_number INT NOT NULL,
                UNIQUE KEY (salong_id, row_num, seat_number),
                FOREIGN KEY (salong_id) REFERENCES Salongs(id)
            );

            -- Ticket_Type
            CREATE TABLE IF NOT EXISTS Ticket_Type (
                id INT PRIMARY KEY AUTO_INCREMENT,
                name VARCHAR(50) NOT NULL,
                price DECIMAL(10, 2) NOT NULL
            );

            -- Showings
            CREATE TABLE IF NOT EXISTS Showings (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                salong_id INT NOT NULL,
                start_time DATETIME NOT NULL,
                language VARCHAR(50),
                subtitle VARCHAR(50),
                FOREIGN KEY (film_id) REFERENCES Films(id),
                FOREIGN KEY (salong_id) REFERENCES Salongs(id)
            );

            -- Bookings
            CREATE TABLE IF NOT EXISTS Bookings (
                id INT PRIMARY KEY AUTO_INCREMENT,
                booking_reference VARCHAR(10) NOT NULL UNIQUE,
                email VARCHAR(255) NOT NULL,
                showing_id INT NOT NULL,
                booked_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (showing_id) REFERENCES Showings(id)
            );

            -- Booked_Seats
            CREATE TABLE IF NOT EXISTS Booked_Seats (
                id INT PRIMARY KEY AUTO_INCREMENT,
                seat_id INT NOT NULL,
                showing_id INT NOT NULL,
                booking_id INT NOT NULL,
                ticket_type_id INT NOT NULL,
                UNIQUE KEY (seat_id, showing_id),
                FOREIGN KEY (seat_id) REFERENCES Seats(id),
                FOREIGN KEY (showing_id) REFERENCES Showings(id),
                FOREIGN KEY (booking_id) REFERENCES Bookings(id),
                FOREIGN KEY (ticket_type_id) REFERENCES Ticket_Type(id)
            );

            -- Contacts
            CREATE TABLE IF NOT EXISTS Contacts (
                id INT PRIMARY KEY AUTO_INCREMENT,
                name VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL,
                subject VARCHAR(255) NOT NULL,
                message TEXT NOT NULL,
                status ENUM('unread', 'read') DEFAULT 'unread' NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );
        ";

        // Execute each statement separately
        foreach (var sql in createTablesSql.Split(';'))
        {
            var trimmed = sql.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                var command = db.CreateCommand();
                command.CommandText = trimmed;
                command.ExecuteNonQuery();
            }
        }
    }

    private static void CreateViews(MySqlConnection db)
    {
        var createViewSql = @"
            CREATE OR REPLACE VIEW showings_detail AS
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
            JOIN Salongs sa ON s.salong_id = sa.id
        ";

        var command = db.CreateCommand();
        command.CommandText = createViewSql;
        command.ExecuteNonQuery();

        var bookingDetailsViewSql = @"
            CREATE OR REPLACE VIEW booking_details AS
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
                     f.age_rating, f.genre, f.images, sa.name
        ";

        var command2 = db.CreateCommand();
        command2.CommandText = bookingDetailsViewSql;
        command2.ExecuteNonQuery();
    }

    private static void SeedDataIfEmpty(MySqlConnection db)
    {
        var command = db.CreateCommand();

        // -------------------------
        // Seed ACL rules
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM acl";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            var aclData = @"
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

            ('admin',               '*',   'allow', '/api/admin/*',       'true', 'Admin full access to admin routes')
            ";

            command.CommandText = aclData;
            command.ExecuteNonQuery();

            // DEBUG ACL RULES
            command.CommandText = "SELECT COUNT(*) FROM acl";
            Console.WriteLine("ACL rows after seed: " + Convert.ToInt32(command.ExecuteScalar()));
        }

        // -------------------------
        // Seed admin user
        // -------------------------
        command.Parameters.Clear();
        command.CommandText = "SELECT COUNT(*) FROM Users WHERE email = @email";
        command.Parameters.AddWithValue("@email", "admin@cinemamob.com");

        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            var adminPassword = Password.Encrypt("admin123");

            command.CommandText = @"
                INSERT INTO Users (email, firstName, lastName, role, password)
                VALUES (@email, @firstName, @lastName, @role, @password)
            ";

            command.Parameters.Clear();
            command.Parameters.AddWithValue("@email", "admin@cinemamob.com");
            command.Parameters.AddWithValue("@firstName", "Admin");
            command.Parameters.AddWithValue("@lastName", "User");
            command.Parameters.AddWithValue("@role", "admin");
            command.Parameters.AddWithValue("@password", adminPassword);
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed domain data
        // -------------------------
        command.Parameters.Clear();
        command.CommandText = "SELECT COUNT(*) FROM Films";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            var seedData = @"
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
                '[""https://m.media-amazon.com/images/M/MV5BNGEwYjgwOGQtYjg5ZS00Njc1LTk2ZGEtM2QwZWQ2NjdhZTE5XkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=sY1S34973zA""]'),

                ('Goodfellas', 'En ung man växer upp i den italiensk-amerikanska maffian.', 146, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BN2E5NzI2ZGMtY2VjNi00YTRjLWI1MDUtZGY5OWU1MWJjZjRjXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=qo5jJpHtI1Y""]'),

                ('Scarface', 'Tony Montana bygger upp ett brutalt kokainimperium i Miami.', 170, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BNDUzYjY0NmUtMDM4OS00Y2Q5LWJiODYtNTk0ZTk0YjZhMTg1XkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=7pQQHnqBa2E""]'),

                ('Casino', 'Historien om maffians kontroll över Las Vegas.', 178, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BMDRlZWZjZjYtYzY2NS00ZWVjLTkwYzAtZTA2ZDAzMGRiYmYwXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=EJXDMwGWhoA""]'),

                ('The Departed', 'En polis infiltrerar maffian samtidigt som en gangster infiltrerar polisen.', 151, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BMTI1MTY2OTIxNV5BMl5BanBnXkFtZTYwNjQ4NjY3._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=iojhqm0JTW4""]'),

                ('American Gangster', 'Frank Lucas bygger ett heroinimperium i Harlem.', 157, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BZGExM2MwNjUtNThkNi00ZjBmLWJhZDgtN2ZmOWJiZWEwNGMxXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=BV_nssS6Zkg""]'),

                ('The Irishman', 'En hitman berättar historien om sitt liv inom maffian.', 209, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BMTY2YThkNmQtOWJhYy00ZDc3LWEzOGEtMmQwNzM0YjFmZWIyXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=WHXxVmeGQUc""]'),

                ('The Gentlemen', 'En brittisk cannabisbaron försöker sälja sitt imperium.', 113, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BMjE2ZjQ4ZGMtZjFhMi00NmI5LTliNjEtODczMWMxNjliZjgxXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=2B0RpUGss2c""]'),

                ('The Boss Baby', 'En bebis som egentligen är en hemlig agent försöker stoppa en ond plan.', 97, '0', 'Animerat',
                '[""https://m.media-amazon.com/images/M/MV5BOWEwYWY5NWItMDQ0NS00MjkzLWE1MDEtNTQyYTQ3MmM1NTUyXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=tquIfapGVqs""]'),

                ('Zootopia', 'En polis-kanin och en småkriminell räv löser ett mysterium.', 108, '7', 'Animerat',
                '[""https://m.media-amazon.com/images/M/MV5BOTMyMjEyNzIzMV5BMl5BanBnXkFtZTgwNzIyNjU0NzE@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=jWM0ct-OLsM""]'),

                ('Hotel Transylvania', 'Monster driver ett hotell där människor egentligen inte får komma in.', 91, '7', 'Familj',
                '[""https://m.media-amazon.com/images/M/MV5BMTM3NjQyODI3M15BMl5BanBnXkFtZTcwMDM4NjM0OA@@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=q4RK3jY7AVk""]'),

                ('Beetlejuice', 'Ett spökpar försöker skrämma bort nya invånare från sitt hus.', 92, '11', 'Skräck',
                '[""https://m.media-amazon.com/images/M/MV5BYTQxZTUzOTgtZmU4MC00NTc3LTkyMDMtYzNkYzAwMzc4NWQxXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=ickbVzajrk0""]'),

                ('Ghostbusters', 'Tre forskare startar en verksamhet som jagar spöken.', 105, '11', 'Komedi',
                '[""https://m.media-amazon.com/images/M/MV5BMGI0Yjg2ODAtNDYzNi00Njc2LTlkMmMtMmRmYWI5MDE4ZGRkXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=vntAEVjPBzQ""]'),

                ('Donnie Brasco', 'En FBI-agent infiltrerar maffian och kommer farligt nära sitt mål.', 147, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BODBlNDIxZjgtYzI1Mi00MWNlLWI1NjYtNzY2M2IzYjgyOGFkXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=omIiE9KKj2o""]'),

                ('Public Enemies', 'Historien om bankrånaren John Dillinger under 1930-talet.', 140, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BMThiZTM5YTMtMzczMC00ZTRjLWE1NzQtMDA2MzViMmYwNjc3XkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=Ee92mDZu_PI""]'),

                ('Gangs of New York', 'Gängkrig bryter ut i New York under 1800-talet.', 167, '15', 'Krim',
                '[""https://m.media-amazon.com/images/M/MV5BMmJmM2Q3MDYtMmFiNy00NTM2LWEzZGQtZDJmMGUyM2QwNTQxXkEyXkFqcGc@._V1_.jpg""]',
                '[""https://www.youtube.com/watch?v=qHVUPri5tjA""]');


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
            ";

            // Execute each statement separately
            foreach (var sql in seedData.Split(';'))
            {
                var trimmed = sql.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    var command2 = db.CreateCommand();
                    command2.CommandText = trimmed;
                    command2.ExecuteNonQuery();
                }
            }
        }
    }

    // Helper to create an object from the DataReader
    private static dynamic ObjFromReader(MySqlDataReader reader)
    {
        var obj = Obj();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var key = reader.GetName(i);
            var value = reader.GetValue(i);

            // Handle NULL values
            if (value == DBNull.Value)
            {
                obj[key] = null;
            }
            // Handle DateTime - convert to ISO string
            else if (value is DateTime dt)
            {
                obj[key] = dt.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            // Handle boolean (MySQL returns sbyte for TINYINT(1))
            else if (value is sbyte sb)
            {
                obj[key] = sb != 0;
            }
            else if (value is bool b)
            {
                obj[key] = b;
            }
            // Handle JSON columns (MySQL returns JSON as string starting with [ or {)
            else if (value is string strValue && (strValue.StartsWith("[") || strValue.StartsWith("{")))
            {
                // Special case: Don't parse 'data' column from sessions - keep as string
                if (key == "data")
                {
                    obj[key] = strValue;
                }
                else
                {
                    try
                    {
                        obj[key] = JSON.Parse(strValue);
                    }
                    catch
                    {
                        // If parsing fails, keep the original value and try to convert to number
                        obj[key] = strValue.TryToNum();
                    }
                }
            }
            else
            {
                // Normal handling - convert to string and try to parse as number
                obj[key] = value.ToString().TryToNum();
            }
        }
        return obj;
    }

    // Run a query - rows are returned as an array of objects
    public static Arr SQLQuery(
        string sql, object parameters = null, HttpContext context = null
    )
    {
        var paras = parameters == null ? Obj() : Obj(parameters);

        using var db = new MySqlConnection(connectionString);
        db.Open();

        // IMPORTANT: Trim to avoid "\r\nSELECT ..." being treated as non-query
        var sqlTrimmed = (sql ?? "").Trim();

        var command = db.CreateCommand();
        command.CommandText = sqlTrimmed;

        // Add parameters
        var entries = (Arr)paras.GetEntries();
        entries.ForEach(x => command.Parameters.AddWithValue("@" + x[0], x[1]));

        if (context != null)
        {
            DebugLog.Add(context, new
            {
                sqlQuery = Regex.Replace(sqlTrimmed, @"\s+", " "),
                sqlParams = paras
            });
        }

        var rows = Arr();

        try
        {
            // Robust detection (after Trim)
            var isSelect = sqlTrimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
            var isCall = sqlTrimmed.StartsWith("CALL", StringComparison.OrdinalIgnoreCase);
            var isShow = sqlTrimmed.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase);
            var isDescribe = sqlTrimmed.StartsWith("DESCRIBE", StringComparison.OrdinalIgnoreCase);
            var isExplain = sqlTrimmed.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase);

            if (isSelect || isCall || isShow || isDescribe || isExplain)
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    rows.Push(ObjFromReader(reader));
            }
            else
            {
                rows.Push(new
                {
                    command = sqlTrimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant(),
                    rowsAffected = command.ExecuteNonQuery()
                });
            }
        }
        catch (Exception err)
        {
            rows.Push(new { error = err.Message });
        }

        return rows;
    }

    // Run a query - only return the first row, as an object
    public static dynamic SQLQueryOne(
        string sql, object parameters = null, HttpContext context = null
    )
    {
        return SQLQuery(sql, parameters, context)[0];
    }

    private static void CreateStoredProcedures(MySqlConnection db)
    {
        var dropCommand = db.CreateCommand();
        dropCommand.CommandText = @"DROP PROCEDURE IF EXISTS CreateBookingWithSeats";
        dropCommand.ExecuteNonQuery();
        dropCommand.CommandText = @"DROP PROCEDURE IF EXISTS DeleteBooking";
        dropCommand.ExecuteNonQuery();

        var createCommand = db.CreateCommand();
        createCommand.CommandText = @"
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
        END
        ";
        createCommand.ExecuteNonQuery();

        createCommand.CommandText = @"
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
        END
        ";
        createCommand.ExecuteNonQuery();
    }
}