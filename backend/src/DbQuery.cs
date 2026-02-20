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
                ('Inception', 'En tjuv som stjäl företagshemligheter genom drömdelning får en chans att radera sitt förflutna.', 148, '15', 'Sci-Fi', '[""https://m.media-amazon.com/images/M/MV5BMjAxMzY3NjcxNF5BMl5BanBnXkFtZTcwNTI5OTM0Mw@@._V1_.jpg"", ""inception2.jpg""]', '[""https://www.youtube.com/watch?v=YoHD9XEInc0""]'),
                ('Parasite', 'En fattig familj infiltrerar en rik familj med oväntade konsekvenser.', 132, '15', 'Thriller', '[""https://m.media-amazon.com/images/M/MV5BYjk1Y2U4MjQtY2ZiNS00OWQyLWI3MmYtZWUwNmRjYWRiNWNhXkEyXkFqcGc@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=5xH0HfJHsaY""]'),
                ('Toy Story 4', 'Woody och gänget ger sig ut på ett nytt äventyr.', 100, '0', 'Animerat', '[""https://m.media-amazon.com/images/M/MV5BMTYzMDM4NzkxOV5BMl5BanBnXkFtZTgwNzM1Mzg2NzM@._V1_.jpg"", ""toystory2.jpg""]', '[""https://youtube.com/toystory4""]'),
                ('The Godfather', 'En maffiafamiljs patriark överför kontrollen till sin motvillige son.', 175, '15', 'Drama', '[""https://m.media-amazon.com/images/M/MV5BNGEwYjgwOGQtYjg5ZS00Njc1LTk2ZGEtM2QwZWQ2NjdhZTE5XkEyXkFqcGc@._V1_.jpg""]', '[""https://youtube.com/godfather""]'),
                ('Spirited Away', 'En flicka hamnar i en värld av gudar och andar.', 125, '7', 'Animerat', '[""https://m.media-amazon.com/images/M/MV5BNTEyNmEwOWUtYzkyOC00ZTQ4LTllZmUtMjk0Y2YwOGUzYjRiXkEyXkFqcGc@._V1_.jpg"", ""spirited2.jpg""]', '[""https://youtube.com/spiritedaway""]'),
                ('Dune', 'Paul Atreides reser till den farligaste planeten i universum.', 155, '11', 'Sci-Fi', '[""https://m.media-amazon.com/images/M/MV5BNWIyNmU5MGYtZDZmNi00ZjAwLWJlYjgtZTc0ZGIxMDE4ZGYwXkEyXkFqcGc@._V1_.jpg""]', '[""https://youtube.com/dune""]'),
                ('Interstellar', 'En grupp astronauter reser genom ett maskhål i rymden för att rädda mänskligheten.', 169, '11', 'Sci-Fi', '[""https://m.media-amazon.com/images/M/MV5BYzdjMDAxZGItMjI2My00ODA1LTlkNzItOWFjMDU5ZDJlYWY3XkEyXkFqcGc@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=zSWdZVtXT7E""]'),
                ('The Dark Knight', 'Batman ställs mot Jokern i en kamp om stadens själ.', 152, '15', 'Action', '[""https://m.media-amazon.com/images/M/MV5BMTMxNTMwODM0NF5BMl5BanBnXkFtZTcwODAyMTk2Mw@@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=kmJLuwP3MbY""]'),
                ('Coco', 'En pojke reser till de dödas rike för att upptäcka sin familjs historia.', 105, '7', 'Animerat', '[""https://m.media-amazon.com/images/M/MV5BMDIyM2E2NTAtMzlhNy00ZGUxLWI1NjgtZDY5MzhiMDc5NGU3XkEyXkFqcGc@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=xlnPHQ3TLX8""]'),
                ('The Grand Budapest Hotel', 'En excentrisk concierge och hans lobby boy dras in i ett mordmysterium.', 99, '11', 'Komedi', '[""https://m.media-amazon.com/images/M/MV5BMzM5NjUxOTEyMl5BMl5BanBnXkFtZTgwNjEyMDM0MDE@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=1Fg5iWmQjwk""]'),
                ('Arrival', 'En lingvist försöker kommunicera med utomjordingar som landat på jorden.', 116, '11', 'Sci-Fi', '[""https://m.media-amazon.com/images/M/MV5BMTExMzU0ODcxNDheQTJeQWpwZ15BbWU4MDE1OTI4MzAy._V1_.jpg""]', '[""https://www.youtube.com/watch?v=tFMo3UJ4B4g""]'),
                ('The Pursuit of Happyness', 'En ensamstående pappa kämpar för att skapa ett bättre liv för sin son.', 117, '7', 'Drama', '[""https://m.media-amazon.com/images/M/MV5BMTQ5NjQ0NDI3NF5BMl5BanBnXkFtZTcwNDI0MjEzMw@@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=DMOBlEcRuw8""]'),
                ('Mad Max: Fury Road', 'I en postapokalyptisk öken försöker en grupp fly från en tyrannisk härskare.', 120, '15', 'Action', '[""https://m.media-amazon.com/images/M/MV5BZDRkODJhOTgtOTc1OC00NTgzLTk4NjItNDgxZDY4YjlmNDY2XkEyXkFqcGc@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=hEJnMQG9ev8""]'),
                ('Paddington 2', 'En charmig björn hamnar i trubbel när han försöker köpa en speciell present.', 103, '7', 'Familj', '[""https://m.media-amazon.com/images/M/MV5BNTk1YzlhMTUtZmU5MC00NmRmLTlkZjItYzQ0NTY4Y2NiNzc4XkEyXkFqcGc@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=sw7RElt-SvE""]'),
                ('Her', 'En man utvecklar en oväntad relation med ett avancerat operativsystem.', 126, '11', 'Drama', '[""https://m.media-amazon.com/images/M/MV5BMjA1Nzk0OTM2OF5BMl5BanBnXkFtZTgwNjU2NjEwMDE@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=dJTU48_yghs""]'),
                ('Knives Out', 'En berömd deckarförfattare hittas död och en detektiv utreder familjen.', 130, '11', 'Mysterium', '[""https://m.media-amazon.com/images/M/MV5BZDU5ZTRkYmItZjg0Mi00ZTQwLThjMWItNWM3MTMxMzVjZmVjXkEyXkFqcGc@._V1_.jpg""]', '[""https://www.youtube.com/watch?v=gj5ibYSz8C0""]');


                -- Directors
                INSERT INTO Directors (film_id, name) VALUES
                (1, 'Christopher Nolan'),
                (2, 'Bong Joon-ho'),
                (3, 'Josh Cooley'),
                (4, 'Francis Ford Coppola'),
                (5, 'Hayao Miyazaki'),
                (6, 'Denis Villeneuve'),
                (7, 'Christopher Nolan'),
                (8, 'Christopher Nolan'),
                (9, 'Lee Unkrich'),
                (10, 'Wes Anderson'),
                (11, 'Denis Villeneuve'),
                (12, 'Gabriele Muccino'),
                (13, 'George Miller'),
                (14, 'Paul King'),
                (15, 'Spike Jonze'),
                (16, 'Rian Johnson');


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
                (6, 'Zendaya', 2),

                (7, 'Matthew McConaughey', 1),
                (7, 'Anne Hathaway', 2),
                (7, 'Jessica Chastain', 3),

                (8, 'Christian Bale', 1),
                (8, 'Heath Ledger', 2),
                (8, 'Aaron Eckhart', 3),

                (9, 'Anthony Gonzalez', 1),
                (9, 'Gael García Bernal', 2),

                (10, 'Ralph Fiennes', 1),
                (10, 'Tony Revolori', 2),
                (10, 'Saoirse Ronan', 3),

                (11, 'Amy Adams', 1),
                (11, 'Jeremy Renner', 2),

                (12, 'Will Smith', 1),
                (12, 'Jaden Smith', 2),

                (13, 'Tom Hardy', 1),
                (13, 'Charlize Theron', 2),
                (13, 'Nicholas Hoult', 3),

                (14, 'Ben Whishaw', 1),
                (14, 'Hugh Bonneville', 2),

                (15, 'Joaquin Phoenix', 1),
                (15, 'Scarlett Johansson', 2),
                (15, 'Amy Adams', 3),

                (16, 'Daniel Craig', 1),
                (16, 'Ana de Armas', 2),
                (16, 'Chris Evans', 3);

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
                (6, 1, '2025-02-11 19:00:00', 'Engelska', 'Svenska'),
                (4, 2, '2025-02-12 18:00:00', 'Engelska', 'Svenska'),
                (5, 1, '2025-02-12 15:00:00', 'Japanska', 'Svenska'),
                (7, 1, '2025-02-12 20:00:00', 'Engelska', 'Svenska'),
                (8, 2, '2025-02-13 18:00:00', 'Engelska', 'Svenska'),
                (9, 1, '2025-02-13 14:00:00', 'Engelska', 'Svenska'),
                (10, 2, '2025-02-13 20:00:00', 'Engelska', 'Svenska'),
                (11, 1, '2025-02-14 18:00:00', 'Engelska', 'Svenska'),
                (12, 2, '2025-02-14 15:00:00', 'Engelska', 'Svenska'),
                (13, 1, '2025-02-14 21:00:00', 'Engelska', NULL),
                (14, 2, '2025-02-15 11:00:00', 'Engelska', 'Svenska'),
                (15, 1, '2025-02-15 18:00:00', 'Engelska', 'Svenska'),
                (16, 2, '2025-02-15 20:00:00', 'Engelska', 'Svenska'),
                (1, 2, '2025-02-15 15:00:00', 'Engelska', NULL),
                (2, 1, '2025-02-16 18:00:00', 'Koreanska', 'Svenska'),
                (3, 2, '2025-02-16 11:00:00', 'Svenska', NULL),
                (6, 2, '2025-02-16 20:00:00', 'Engelska', 'Svenska');

                -- Bookings (nästan full showing 1 = Inception i Stora Salongen)
                -- Lediga: rad3 p5-7 (id 22,23,24), rad4 p5,6,8 (id 32,33,35),
                --         rad7 p10-12 (id 67,68,69), rad8 p10-12 (id 79,80,81)
                INSERT INTO Bookings (email, showing_id) VALUES
                ('anna.svensson@email.se', 1),
                ('erik.johansson@email.se', 1),
                ('lisa.andersson@email.se', 1);

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
        dropCommand.CommandText = @"DROP PROCEDURE IF EXISTS CreateBookingWithSeats";
        dropCommand.ExecuteNonQuery();
        dropCommand.CommandText = @"DROP PROCEDURE IF EXISTS DeleteBooking";
        dropCommand.ExecuteNonQuery();

        var createCommand = db.CreateCommand();
        createCommand.CommandText = @"
        CREATE PROCEDURE CreateBookingWithSeats(
            IN customer_email VARCHAR(255),
            IN selected_showing_id INT,
            IN selected_seats_json JSON
        )
        BEGIN
            DECLARE v_booking_id INT;

            DECLARE EXIT HANDLER FOR SQLEXCEPTION
            BEGIN
                ROLLBACK;
            END;

            START TRANSACTION;

            INSERT INTO Bookings (email, showing_id) VALUES (customer_email, selected_showing_id);
            SET v_booking_id = LAST_INSERT_ID();

            INSERT INTO Booked_Seats (seat_id, showing_id, booking_id, ticket_type_id)
            SELECT seats.seat_id, selected_showing_id, v_booking_id, seats.ticket_type_id
            FROM JSON_TABLE(selected_seats_json, '$[*]' COLUMNS(
                seat_id INT PATH '$.seat_id',
                ticket_type_id INT PATH '$.ticket_type_id'
            )) AS seats;

            COMMIT;

            SELECT v_booking_id AS bookingId;
        END
        ";
        createCommand.ExecuteNonQuery();

        createCommand.CommandText = @"
        CREATE PROCEDURE DeleteBooking(
            IN booking_id_param INT
        )
        BEGIN
            DECLARE EXIT HANDLER FOR SQLEXCEPTION
            BEGIN
                ROLLBACK;
            END;

            START TRANSACTION;

            DELETE FROM Booked_Seats
            WHERE booking_id = booking_id_param;

            DELETE FROM Bookings
            WHERE id = booking_id_param;

            COMMIT;
        END
        ";
        createCommand.ExecuteNonQuery();
    }
}
