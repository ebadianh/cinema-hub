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
        }

        db.Close();
    }

    public static void DropTablesIfExist (MySqlConnection db)
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
            CREATE TABLE Users (
                id INT PRIMARY KEY AUTO_INCREMENT,
                created DATETIME DEFAULT CURRENT_TIMESTAMP,
                email VARCHAR(255) NOT NULL UNIQUE,
                firstName VARCHAR(100) NOT NULL,
                lastName VARCHAR(100) NOT NULL,
                role VARCHAR(50) NOT NULL DEFAULT 'user',
                password VARCHAR(255) NOT NULL
            );

            -- Films
            CREATE TABLE Films (
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
            CREATE TABLE Directors (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                name VARCHAR(255) NOT NULL,
                FOREIGN KEY (film_id) REFERENCES Films(id) ON DELETE CASCADE
            );

            -- Actors
            CREATE TABLE Actors (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                name VARCHAR(255) NOT NULL,
                role_order INT NULL,
                FOREIGN KEY (film_id) REFERENCES Films(id) ON DELETE CASCADE
            );

            -- Reviews
            CREATE TABLE Reviews (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                source VARCHAR(100) NOT NULL,
                quote VARCHAR(255) NOT NULL,
                stars INT NOT NULL,
                max_stars INT NOT NULL,
                FOREIGN KEY (film_id) REFERENCES Films(id) ON DELETE CASCADE
            );

            -- Salongs
            CREATE TABLE Salongs (
                id INT PRIMARY KEY AUTO_INCREMENT,
                name VARCHAR(100) NOT NULL
            );

            -- Seats
            CREATE TABLE Seats (
                salong_id INT NOT NULL,
                row_num INT NOT NULL,
                seat_number INT NOT NULL,
                PRIMARY KEY (salong_id, row_num, seat_number),
                FOREIGN KEY (salong_id) REFERENCES Salongs(id)
            );

            -- Ticket_Type
            CREATE TABLE Ticket_Type (
                id INT PRIMARY KEY AUTO_INCREMENT,
                name VARCHAR(50) NOT NULL,
                price DECIMAL(10, 2) NOT NULL
            );

            -- Showings
            CREATE TABLE Showings (
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
            CREATE TABLE Bookings (
                id INT PRIMARY KEY AUTO_INCREMENT,
                email VARCHAR(255) NOT NULL,
                showing_id INT NOT NULL,
                booked_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (showing_id) REFERENCES Showings(id)
            );

            -- Booked_Seats
            CREATE TABLE Booked_Seats (
                salong_id INT NOT NULL,
                row_num INT NOT NULL,
                seat_number INT NOT NULL,
                showing_id INT NOT NULL,
                booking_id INT NOT NULL,
                ticket_type_id INT NOT NULL,
                PRIMARY KEY (salong_id, row_num, seat_number, showing_id),
                FOREIGN KEY (salong_id, row_num, seat_number) REFERENCES Seats(salong_id, row_num, seat_number),
                FOREIGN KEY (showing_id) REFERENCES Showings(id),
                FOREIGN KEY (booking_id) REFERENCES Bookings(id),
                FOREIGN KEY (ticket_type_id) REFERENCES Ticket_Type(id)
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

    private static void SeedDataIfEmpty(MySqlConnection db)
    {
        var command = db.CreateCommand();

        // -------------------------
        // Seed ACL rules
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM acl";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            // Basic rules: allow login + registration, allow GET on public api,
            // admin can do everything under /api
            var aclData = @"
                INSERT INTO acl (userRoles, method, allow, route, `match`, comment) VALUES
                ('visitor,user,admin', '*', 'allow', '/api/login', 'true', 'Allow login routes'),
                ('visitor', 'POST', 'allow', '/api/users', 'true', 'Allow registration for visitors'),
                ('visitor,user,admin', 'GET', 'allow', '/api', 'false', 'Allow GET for non-API routes, deny elsewhere by app logic'),
                ('visitor,user,admin', 'GET', 'allow', '/api/films', 'true', 'Allow all to read films'),
                ('visitor,user,admin', 'GET', 'allow', '/api/showings', 'true', 'Allow all to read showings'),
                ('visitor,user,admin', 'GET', 'allow', '/api/seats', 'true', 'Allow all to read seats'),
                ('user,admin', '*', 'allow', '/api/bookings', 'true', 'Allow logged-in users to manage bookings'),
                ('admin', '*', 'allow', '/api', 'true', 'Admins can access all API routes'),
                ('admin', '*', 'allow', '/api/acl', 'true', 'Allow admins to manage ACL'),
                ('admin', '*', 'allow', '/api/sessions', 'true', 'Allow admins to manage sessions');
            ";
            command.CommandText = aclData;
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed users
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM users";
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

                -- Seats för Stora Salongen (id 1): 7 rader, varierande 6-8 säten
                INSERT INTO Seats (salong_id, row_num, seat_number) VALUES
                (1, 1, 1), (1, 1, 2), (1, 1, 3), (1, 1, 4), (1, 1, 5), (1, 1, 6),
                (1, 2, 1), (1, 2, 2), (1, 2, 3), (1, 2, 4), (1, 2, 5), (1, 2, 6), (1, 2, 7),
                (1, 3, 1), (1, 3, 2), (1, 3, 3), (1, 3, 4), (1, 3, 5), (1, 3, 6), (1, 3, 7), (1, 3, 8),
                (1, 4, 1), (1, 4, 2), (1, 4, 3), (1, 4, 4), (1, 4, 5), (1, 4, 6), (1, 4, 7), (1, 4, 8),
                (1, 5, 1), (1, 5, 2), (1, 5, 3), (1, 5, 4), (1, 5, 5), (1, 5, 6), (1, 5, 7), (1, 5, 8),
                (1, 6, 1), (1, 6, 2), (1, 6, 3), (1, 6, 4), (1, 6, 5), (1, 6, 6), (1, 6, 7),
                (1, 7, 1), (1, 7, 2), (1, 7, 3), (1, 7, 4), (1, 7, 5), (1, 7, 6);

                -- Seats för Lilla Salongen (id 2): 4 rader, 5 säten per rad
                INSERT INTO Seats (salong_id, row_num, seat_number) VALUES
                (2, 1, 1), (2, 1, 2), (2, 1, 3), (2, 1, 4), (2, 1, 5),
                (2, 2, 1), (2, 2, 2), (2, 2, 3), (2, 2, 4), (2, 2, 5),
                (2, 3, 1), (2, 3, 2), (2, 3, 3), (2, 3, 4), (2, 3, 5),
                (2, 4, 1), (2, 4, 2), (2, 4, 3), (2, 4, 4), (2, 4, 5);

                -- Films
                INSERT INTO Films (title, description, duration_minutes, age_rating, genre, images, trailers) VALUES
                ('Inception', 'En tjuv som stjäl företagshemligheter genom drömdelning får en chans att radera sitt förflutna.', 148, '15', 'Sci-Fi', '[""inception1.jpg"", ""inception2.jpg""]', '[""https://youtube.com/inception""]'),
                ('Parasite', 'En fattig familj infiltrerar en rik familj med oväntade konsekvenser.', 132, '15', 'Thriller', '[""parasite1.jpg""]', '[""https://youtube.com/parasite""]'),
                ('Toy Story 4', 'Woody och gänget ger sig ut på ett nytt äventyr.', 100, 'Alla', 'Animerat', '[""toystory1.jpg"", ""toystory2.jpg""]', '[""https://youtube.com/toystory4""]'),
                ('The Godfather', 'En maffiafamiljs patriark överför kontrollen till sin motvillige son.', 175, '15', 'Drama', '[""godfather1.jpg""]', '[""https://youtube.com/godfather""]'),
                ('Spirited Away', 'En flicka hamnar i en värld av gudar och andar.', 125, '7', 'Animerat', '[""spirited1.jpg"", ""spirited2.jpg""]', '[""https://youtube.com/spiritedaway""]'),
                ('Dune', 'Paul Atreides reser till den farligaste planeten i universum.', 155, '11', 'Sci-Fi', '[""dune1.jpg""]', '[""https://youtube.com/dune""]');

                -- Directors
                INSERT INTO Directors (film_id, name) VALUES
                (1, 'Christopher Nolan'),
                (2, 'Bong Joon-ho'),
                (3, 'Josh Cooley'),
                (4, 'Francis Ford Coppola'),
                (5, 'Hayao Miyazaki'),
                (6, 'Denis Villeneuve');

                -- Actors
                INSERT INTO Actors (film_id, name, role_order) VALUES
                (1, 'Leonardo DiCaprio', 1),
                (1, 'Joseph Gordon-Levitt', 2),
                (1, 'Elliot Page', 3),
                (2, 'Song Kang-ho', 1),
                (2, 'Choi Woo-shik', 2),
                (3, 'Tom Hanks', 1),
                (3, 'Tim Allen', 2),
                (4, 'Marlon Brando', 1),
                (4, 'Al Pacino', 2),
                (5, 'Rumi Hiiragi', 1),
                (5, 'Miyu Irino', 2),
                (6, 'Timothée Chalamet', 1),
                (6, 'Zendaya', 2);

                -- Reviews (några filmer med 2-3, några med 0)
                INSERT INTO Reviews (film_id, source, quote, stars, max_stars) VALUES
                (1, 'IMDb', 'Ett mästerverk av visuell berättarkonst.', 9, 10),
                (1, 'Rotten Tomatoes', 'Nolan levererar igen.', 4, 5),
                (1, 'Aftonbladet', 'Hjärnvriden och briljant.', 5, 5),
                (2, 'IMDb', 'En unik och oförglömlig film.', 9, 10),
                (2, 'Svenska Dagbladet', 'Samhällskritik när den är som bäst.', 5, 5),
                (4, 'IMDb', 'En tidlös klassiker.', 10, 10),
                (4, 'Rotten Tomatoes', 'Filmhistoriens bästa.', 5, 5),
                (4, 'Expressen', 'Brando i toppform.', 4, 5);
                -- Film 3, 5 och 6 har inga reviews

                -- Showings
                INSERT INTO Showings (film_id, salong_id, start_time, language, subtitle) VALUES
                (1, 1, '2025-02-10 18:00:00', 'Engelska', 'Svenska'),
                (2, 2, '2025-02-10 20:00:00', 'Koreanska', 'Svenska'),
                (3, 1, '2025-02-11 14:00:00', 'Svenska', NULL),
                (6, 1, '2025-02-11 19:00:00', 'Engelska', 'Svenska');


                -- Bookings
                INSERT INTO Bookings (email, showing_id) VALUES
                ('anna.svensson@email.se', 1);

                -- Booked_Seats
                INSERT INTO Booked_Seats (salong_id, row_num, seat_number, showing_id, booking_id, ticket_type_id) VALUES
                (1, 3, 4, 1, 1, 1),
                (1, 3, 5, 1, 1, 1),
                (1, 3, 6, 1, 1, 3);
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
        var command = db.CreateCommand();
        command.CommandText = @sql;
        var entries = (Arr)paras.GetEntries();
        entries.ForEach(x => command.Parameters.AddWithValue("@" + x[0], x[1]));
        if (context != null)
        {
            DebugLog.Add(context, new
            {
                sqlQuery = sql.Regplace(@"\s+", " "),
                sqlParams = paras
            });
        }
        var rows = Arr();
        try
        {
            if (sql.StartsWith("SELECT ", true, null) || sql.StartsWith("CALL ", true, null))
            {
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rows.Push(ObjFromReader(reader));
                }
                reader.Close();
            }
            else
            {
                rows.Push(new
                {
                    command = sql.Split(" ")[0].ToUpper(),
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
        dropCommand.CommandText = "DROP PROCEDURE IF EXISTS CreateBookingWithSeats";
        dropCommand.ExecuteNonQuery();

        var createCommand = db.CreateCommand();
        createCommand.CommandText = @"
        CREATE PROCEDURE CreateBookingWithSeats(
            IN customer_email VARCHAR(255),
            IN selected_showing_id INT,
            IN selected_seats_json JSON
        )
        BEGIN
            DECLARE v_salong_id INT;
            DECLARE v_booking_id INT;

            DECLARE EXIT HANDLER FOR SQLEXCEPTION
            BEGIN
                ROLLBACK;
            END;

            START TRANSACTION;

            SELECT salong_id INTO v_salong_id FROM Showings WHERE id = selected_showing_id;

            INSERT INTO Bookings (email, showing_id) VALUES (customer_email, selected_showing_id);
            SET v_booking_id = LAST_INSERT_ID();

            INSERT INTO Booked_Seats (salong_id, row_num, seat_number, showing_id, booking_id, ticket_type_id)
            SELECT v_salong_id, seats.row_num, seats.seat_number, selected_showing_id, v_booking_id, seats.ticket_type_id
            FROM JSON_TABLE(selected_seats_json, '$[*]' COLUMNS(
                row_num INT PATH '$.row',
                seat_number INT PATH '$.seat',
                ticket_type_id INT PATH '$.ticketTypeId'
            )) AS seats;

            COMMIT;

            SELECT v_booking_id AS bookingId;
        END
        ";
     createCommand.ExecuteNonQuery();
    }
}
