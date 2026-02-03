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
            CreateTablesIfNotExist(db);
        }

        // Seed data if tables are empty
        if (config.seedDataIfEmpty == true)
        {
            SeedDataIfEmpty(db);
        }

        db.Close();
    }

    // Execute a batch of SQL statements separated by ';'
    // (Works for our schema/seed blocks where we don't have ';' inside string literals)
    private static void ExecBatch(MySqlConnection db, string sqlBatch)
    {
        foreach (var sql in sqlBatch.Split(';'))
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

            CREATE TABLE IF NOT EXISTS users (
                id INT PRIMARY KEY AUTO_INCREMENT,
                email VARCHAR(255) NOT NULL UNIQUE,
                password VARCHAR(255) NOT NULL,
                role VARCHAR(20) NOT NULL DEFAULT 'user',
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS languages (
                id INT PRIMARY KEY AUTO_INCREMENT,
                code VARCHAR(20) NOT NULL UNIQUE,
                name VARCHAR(50) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS subtitles (
                id INT PRIMARY KEY AUTO_INCREMENT,
                code VARCHAR(20) NOT NULL UNIQUE,
                name VARCHAR(50) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS films (
                id INT PRIMARY KEY AUTO_INCREMENT,
                title VARCHAR(255) NOT NULL,
                production_year INT NOT NULL,
                length_minutes INT NOT NULL,
                genre VARCHAR(100) NOT NULL,
                distributor VARCHAR(100) NULL,
                language_id INT NOT NULL,
                subtitle_id INT NOT NULL,
                age_limit INT NULL,
                description TEXT NULL,
                director VARCHAR(255) NULL,
                actors TEXT NULL,
                CONSTRAINT fk_films_language
                    FOREIGN KEY (language_id) REFERENCES languages(id) ON DELETE RESTRICT,
                CONSTRAINT fk_films_subtitle
                    FOREIGN KEY (subtitle_id) REFERENCES subtitles(id) ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS reviews (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                source VARCHAR(100) NOT NULL,
                quote VARCHAR(255) NOT NULL,
                stars INT NOT NULL,
                max_stars INT NOT NULL,
                CONSTRAINT fk_reviews_films
                    FOREIGN KEY (film_id) REFERENCES films(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS images (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                image_url VARCHAR(500) NOT NULL,
                CONSTRAINT fk_images_films
                    FOREIGN KEY (film_id) REFERENCES films(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS youtube_trailers (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                youtube_id VARCHAR(30) NOT NULL,
                CONSTRAINT fk_youtube_trailers_films
                    FOREIGN KEY (film_id) REFERENCES films(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ticket_types (
                id INT PRIMARY KEY AUTO_INCREMENT,
                code VARCHAR(30) NOT NULL UNIQUE,
                name VARCHAR(50) NOT NULL,
                price DECIMAL(10,2) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS salongs (
                id INT PRIMARY KEY AUTO_INCREMENT,
                name VARCHAR(100) NOT NULL UNIQUE,
                seats_per_row TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS showings (
                id INT PRIMARY KEY AUTO_INCREMENT,
                film_id INT NOT NULL,
                salong_id INT NOT NULL,
                starts_at DATETIME NOT NULL,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UNIQUE (salong_id, starts_at),
                CONSTRAINT fk_showings_films
                    FOREIGN KEY (film_id) REFERENCES films(id) ON DELETE RESTRICT,
                CONSTRAINT fk_showings_salongs
                    FOREIGN KEY (salong_id) REFERENCES salongs(id) ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS bookings (
                id INT PRIMARY KEY AUTO_INCREMENT,
                user_id INT NOT NULL,
                showing_id INT NOT NULL,
                booking_code VARCHAR(36) NOT NULL UNIQUE,
                status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
                total_price DECIMAL(10,2) NOT NULL DEFAULT 0,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                cancelled_at DATETIME NULL,
                CONSTRAINT fk_bookings_users
                    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,
                CONSTRAINT fk_bookings_showings
                    FOREIGN KEY (showing_id) REFERENCES showings(id) ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS seats (
                id INT PRIMARY KEY AUTO_INCREMENT,
                showing_id INT NOT NULL,
                row_no INT NOT NULL,
                seat_in_row INT NOT NULL,
                global_number INT NOT NULL,
                booking_id INT NULL,
                UNIQUE (showing_id, global_number),
                UNIQUE (showing_id, row_no, seat_in_row),
                CONSTRAINT fk_seats_showings
                    FOREIGN KEY (showing_id) REFERENCES showings(id) ON DELETE CASCADE,
                CONSTRAINT fk_seats_bookings
                    FOREIGN KEY (booking_id) REFERENCES bookings(id) ON DELETE SET NULL
            );
        ";

        ExecBatch(db, createTablesSql);
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
            command.CommandText = @"
                INSERT INTO users (email, password, role) VALUES
                ('admin@filmvisarna.se', 'admin123', 'admin'),
                ('alice@example.com', 'password', 'user'),
                ('bob@example.com', 'password', 'user'),
                ('carla@example.com', 'password', 'user')
                ('thomas@nodehill.com', 'Thomas', 'Frank', 'admin', '$2a$13$IahRVtN2pxc1Ne1NzJUPpOQO5JCtDZvXpSF.IF8uW85S6VoZKCwZq');
            ";
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed languages
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM languages";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            command.CommandText = @"
                INSERT INTO languages (code, name) VALUES
                ('sv', 'svenska'),
                ('en', 'engelska'),
                ('ko', 'koreanska'),
                ('fr', 'franska'),
                ('ja', 'japanska');
            ";
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed subtitles
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM subtitles";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            command.CommandText = @"
                INSERT INTO subtitles (code, name) VALUES
                ('sv', 'svenska'),
                ('none', 'inga');
            ";
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed ticket_types
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM ticket_types";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            command.CommandText = @"
                INSERT INTO ticket_types (code, name, price) VALUES
                ('adult',  'Vuxen',     140.00),
                ('senior', 'Pensionär', 120.00),
                ('child',  'Barn',       80.00);
            ";
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed films (only if empty)
        // Uses SET variables => run as batch
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM films";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            var filmsSeed = @"
                SET @lang_en := (SELECT id FROM languages WHERE code='en' LIMIT 1);
                SET @lang_ko := (SELECT id FROM languages WHERE code='ko' LIMIT 1);
                SET @lang_fr := (SELECT id FROM languages WHERE code='fr' LIMIT 1);
                SET @lang_ja := (SELECT id FROM languages WHERE code='ja' LIMIT 1);
                SET @sub_sv  := (SELECT id FROM subtitles WHERE code='sv' LIMIT 1);

                INSERT INTO films
                  (title, production_year, length_minutes, genre, distributor,
                   language_id, subtitle_id, age_limit, description, director, actors)
                VALUES
                ('Call Me by Your Name', 2017, 132, 'Drama', 'UIP',
                 @lang_en, @sub_sv, 11,
                 'En sommar i Italien 1983 där kärlek och identitet ställs på sin spets.',
                 'Luca Guadagnino',
                 'Armie Hammer, Timothée Chalamet, Michael Stuhlbarg'),

                ('Parasite', 2019, 132, 'Thriller', 'CJ Entertainment',
                 @lang_ko, @sub_sv, 15,
                 'En mörk satir om klass och ambition som spårar ur.',
                 'Bong Joon-ho',
                 'Song Kang-ho, Cho Yeo-jeong, Choi Woo-shik'),

                ('The Grand Budapest Hotel', 2014, 100, 'Komedi', 'Fox Searchlight',
                 @lang_en, @sub_sv, 11,
                 'En stiliserad berättelse om lojalitet, konst och kaos på ett hotell.',
                 'Wes Anderson',
                 'Ralph Fiennes, Saoirse Ronan, Tony Revolori'),

                ('Portrait of a Lady on Fire', 2019, 122, 'Drama', 'Pyramide',
                 @lang_fr, @sub_sv, 11,
                 'En intensiv kärlekshistoria där blickar blir löften.',
                 'Céline Sciamma',
                 'Noémie Merlant, Adèle Haenel, Luàna Bajrami'),

                ('Spirited Away', 2001, 125, 'Animation', 'Toho',
                 @lang_ja, @sub_sv, 7,
                 'En magisk resa genom en andevärld där mod formas i tystnad.',
                 'Hayao Miyazaki',
                 'Rumi Hiiragi, Miyu Irino, Mari Natsuki');
            ";
            ExecBatch(db, filmsSeed);
        }

        // -------------------------
        // Seed reviews
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM reviews";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            command.CommandText = @"
                INSERT INTO reviews (film_id, source, quote, stars, max_stars) VALUES
                (1, 'Sydsvenskan', 'ett drama berättat med stor ömhet', 4, 5),
                (1, 'Svenska Dagbladet', 'en film att förälska sig i', 5, 5),
                (2, 'DN', 'en sylvass satir med hjärta', 5, 5),
                (3, 'Kino', 'en elegant och varm fars', 4, 5),
                (5, 'Svenska Dagbladet', 'en tidlös klassiker', 5, 5);
            ";
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed images
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM images";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            command.CommandText = @"
                INSERT INTO images (film_id, image_url) VALUES
                (1, 'callme_poster1.jpg'),
                (1, 'callme_poster2.jpg'),
                (2, 'parasite_poster.jpg'),
                (3, 'gbh_poster.jpg'),
                (4, 'portrait_poster.jpg'),
                (5, 'spiritedaway_poster.jpg');
            ";
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed youtube_trailers
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM youtube_trailers";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            command.CommandText = @"
                INSERT INTO youtube_trailers (film_id, youtube_id) VALUES
                (1, 'Z9AYPxH5NTM'),
                (2, 'SEUXfv87Wpk'),
                (3, '1Fg5iWmQjwk'),
                (4, 'R-fQPTwma9o'),
                (5, 'ByXuk9QqQkk');
            ";
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed salongs
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM salongs";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            command.CommandText = @"
                INSERT INTO salongs (name, seats_per_row) VALUES
                ('Stora Salongen', '8,9,10,10,10,10,12,12'),
                ('Lilla Salongen', '6,8,9,10,10,12');
            ";
            command.ExecuteNonQuery();
        }

        // -------------------------
        // Seed showings + seats (only if showings empty)
        // -------------------------
        command.CommandText = "SELECT COUNT(*) FROM showings";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            var showingsAndSeatsSeed = @"
                CREATE TEMPORARY TABLE numbers_28 (d INT NOT NULL PRIMARY KEY);
                INSERT INTO numbers_28 (d) VALUES
                (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),
                (10),(11),(12),(13),(14),(15),(16),(17),(18),(19),
                (20),(21),(22),(23),(24),(25),(26),(27);

                CREATE TEMPORARY TABLE timeslots_3 (t INT NOT NULL PRIMARY KEY, tm TIME NOT NULL);
                INSERT INTO timeslots_3 (t, tm) VALUES
                (1, '18:00:00'),
                (2, '20:30:00'),
                (3, '22:45:00');

                INSERT INTO showings (film_id, salong_id, starts_at)
                SELECT
                  ((d.d + ts.t) % 5) + 1 AS film_id,
                  ((d.d + ts.t) % 2) + 1 AS salong_id,
                  TIMESTAMP(DATE_ADD('2026-02-03', INTERVAL d.d DAY), ts.tm) AS starts_at
                FROM numbers_28 d
                JOIN timeslots_3 ts
                ORDER BY starts_at;

                CREATE TEMPORARY TABLE salong_row_specs (
                  salong_id INT NOT NULL,
                  row_no INT NOT NULL,
                  seats_in_row INT NOT NULL,
                  PRIMARY KEY (salong_id, row_no)
                );

                INSERT INTO salong_row_specs (salong_id, row_no, seats_in_row) VALUES
                (1, 1, 8),(1, 2, 9),(1, 3, 10),(1, 4, 10),(1, 5, 10),(1, 6, 10),(1, 7, 12),(1, 8, 12),
                (2, 1, 6),(2, 2, 8),(2, 3, 9),(2, 4, 10),(2, 5, 10),(2, 6, 12);

                CREATE TEMPORARY TABLE numbers_12 (n INT NOT NULL PRIMARY KEY);
                INSERT INTO numbers_12 (n) VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12);

                INSERT INTO seats (showing_id, row_no, seat_in_row, global_number, booking_id)
                SELECT
                  sh.id AS showing_id,
                  rs.row_no,
                  n.n AS seat_in_row,
                  ROW_NUMBER() OVER (PARTITION BY sh.id ORDER BY rs.row_no, n.n) AS global_number,
                  NULL AS booking_id
                FROM showings sh
                JOIN salong_row_specs rs ON rs.salong_id = sh.salong_id
                JOIN numbers_12 n ON n.n <= rs.seats_in_row
                ORDER BY sh.id, rs.row_no, n.n;
            ";

            ExecBatch(db, showingsAndSeatsSeed);
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
            if (sql.StartsWith("SELECT ", true, null))
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
}
