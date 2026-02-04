namespace WebApp;

public static class DbQuery
{

    // Setup the database connection from config
    private static string connectionString;

    // JSON columns for _CONTAINS_ validation
    public static Arr JsonColumns = Arr(new[] { "categories" });

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

        // drop db
        db_reset_to_default(db);

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

    public static void db_reset_to_default (MySqlConnection db)
    {
        var dropTablesSQL = @"
        USE cinema_hub;
        DROP TABLE IF EXISTS Bookings;
        DROP TABLE IF EXISTS Showings;
        DROP TABLE IF EXISTS Seats;
        DROP TABLE IF EXISTS Salong_Rows;
        DROP TABLE IF EXISTS Salongs;
        DROP TABLE IF EXISTS Films;
        DROP TABLE IF EXISTS Users;
        DROP TABLE IF EXISTS Ticket_Type;
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
                role VARCHAR(50) NOT NULL,
                password VARCHAR(255) NOT NULL
            );

            -- Films
            CREATE TABLE Films (
                id INT PRIMARY KEY AUTO_INCREMENT,
                title VARCHAR(255) NOT NULL,
                description TEXT,
                duration_minutes INT NOT NULL
            );

            -- Salongs
            CREATE TABLE Salongs (
                id INT PRIMARY KEY AUTO_INCREMENT,
                name VARCHAR(100) NOT NULL
            );

            -- Salong_Rows
            CREATE TABLE Salong_Rows (
                salong_id INT NOT NULL,
                row_num INT NOT NULL,
                capacity INT NOT NULL,
                PRIMARY KEY (salong_id, row_num),
                FOREIGN KEY (salong_id) REFERENCES Salongs(id)
            );

            -- Seats
            CREATE TABLE Seats (
                id INT PRIMARY KEY AUTO_INCREMENT,
                salong_id INT NOT NULL,
                row_num INT NOT NULL,
                seat_number INT NOT NULL,
                FOREIGN KEY (salong_id, row_num) REFERENCES Salong_Rows(salong_id, row_num),
                UNIQUE (salong_id, row_num, seat_number)
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
                FOREIGN KEY (film_id) REFERENCES Films(id),
                FOREIGN KEY (salong_id) REFERENCES Salongs(id)
            );

            -- Bookings
            CREATE TABLE Bookings (
                id INT PRIMARY KEY AUTO_INCREMENT,
                user_id INT NOT NULL,
                showing_id INT NOT NULL,
                seat_id INT NOT NULL,
                ticket_type_id INT NOT NULL,
                booked_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES Users(id),
                FOREIGN KEY (showing_id) REFERENCES Showings(id),
                FOREIGN KEY (seat_id) REFERENCES Seats(id),
                FOREIGN KEY (ticket_type_id) REFERENCES Ticket_Type(id),
                UNIQUE (showing_id, seat_id)
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
        // Check if tables are empty and seed if needed
        var command = db.CreateCommand();

        // Seed ACL rules
        command.CommandText = "SELECT COUNT(*) FROM acl";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            var aclData = @"
                INSERT INTO acl (userRoles, method, allow, route, `match`, comment) VALUES
                ('visitor, user', 'GET', 'disallow', '/secret.html', 'true', 'No access to /secret.html for visitors and normal users'),
                ('visitor,user, admin', 'GET', 'allow', '/api', 'false', 'Allow access to all routes not starting with /api'),
                ('visitor', 'POST', 'allow', '/api/users', 'true', 'Allow registration as new user for visitors'),
                ('visitor, user,admin', '*', 'allow', '/api/login', 'true', 'Allow access to all login routes'),
                ('admin', '*', 'allow', '/api/users', 'true', 'Allow admins to see and edit users'),
                ('admin', '*', 'allow', '/api/sessions', 'true', 'Allow admins to see and edit sessions'),
                ('admin', '*', 'allow', '/api/acl', 'true', 'Allow admins to see and edit acl rules'),
                ('visitor,user,admin', 'GET', 'allow', '/api/products', 'true', 'Allow all user roles to read products');
            ";
            command.CommandText = aclData;
            command.ExecuteNonQuery();
        }

        // Seed users
        command.CommandText = "SELECT COUNT(*) FROM users";
        if (Convert.ToInt32(command.ExecuteScalar()) == 0)
        {
            var seedData = @"
                INSERT INTO users (created, email, firstName, lastName, role, password) VALUES
                ('2024-04-02', 'thomas@nodehill.com', 'Thomas', 'Frank', 'admin', '$2a$13$IahRVtN2pxc1Ne1NzJUPpOQO5JCtDZvXpSF.IF8uW85S6VoZKCwZq'),
                ('2024-04-02', 'olle@nodehill.com', 'Olle', 'Olofsson', 'user', '$2a$13$O2Gs3FME3oA1DAzwE0FkOuMAOOAgRyuvNQq937.cl7D.xq0IjgzN.'),
                ('2024-04-02', 'maria@nodehill.com', 'Maria', 'Mårtensson', 'user', '$2a$13$p4sqCN3V3C1wQXspq4eN0eYwK51ypw7NPL6b6O4lMAOyATJtKqjHS');

                -- Films
                INSERT INTO Films (id, title, description, duration_minutes) VALUES
                (1, 'Inception', 'A thief who steals corporate secrets through dream-sharing technology.', 148),
                (2, 'The Dark Knight', 'Batman faces the Joker in Gotham City.', 152),
                (3, 'Interstellar', 'A team of explorers travel through a wormhole in space.', 169),
                (4, 'Pulp Fiction', 'Various interconnected stories of criminals in Los Angeles.', 154),
                (5, 'The Shawshank Redemption', 'Two imprisoned men bond over a number of years.', 142),
                (6, 'Forrest Gump', 'The life journey of a slow-witted but kind-hearted man.', 142);

                -- Salongs
                INSERT INTO Salongs (id, name) VALUES
                (1, 'Salong 1'),
                (2, 'Salong 2');

                -- Salong_Rows (Salong 1: 5 rader, Salong 2: 4 rader)
                INSERT INTO Salong_Rows (salong_id, row_num, capacity) VALUES
                (1, 1, 8),
                (1, 2, 8),
                (1, 3, 10),
                (1, 4, 10),
                (1, 5, 10),
                (2, 1, 6),
                (2, 2, 6),
                (2, 3, 8),
                (2, 4, 8);

                -- Seats för Salong 1
                -- Rad 1 (8 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (1, 1, 1, 1), (2, 1, 1, 2), (3, 1, 1, 3), (4, 1, 1, 4),
                (5, 1, 1, 5), (6, 1, 1, 6), (7, 1, 1, 7), (8, 1, 1, 8);

                -- Rad 2 (8 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (9, 1, 2, 1), (10, 1, 2, 2), (11, 1, 2, 3), (12, 1, 2, 4),
                (13, 1, 2, 5), (14, 1, 2, 6), (15, 1, 2, 7), (16, 1, 2, 8);

                -- Rad 3 (10 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (17, 1, 3, 1), (18, 1, 3, 2), (19, 1, 3, 3), (20, 1, 3, 4),
                (21, 1, 3, 5), (22, 1, 3, 6), (23, 1, 3, 7), (24, 1, 3, 8),
                (25, 1, 3, 9), (26, 1, 3, 10);

                -- Rad 4 (10 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (27, 1, 4, 1), (28, 1, 4, 2), (29, 1, 4, 3), (30, 1, 4, 4),
                (31, 1, 4, 5), (32, 1, 4, 6), (33, 1, 4, 7), (34, 1, 4, 8),
                (35, 1, 4, 9), (36, 1, 4, 10);

                -- Rad 5 (10 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (37, 1, 5, 1), (38, 1, 5, 2), (39, 1, 5, 3), (40, 1, 5, 4),
                (41, 1, 5, 5), (42, 1, 5, 6), (43, 1, 5, 7), (44, 1, 5, 8),
                (45, 1, 5, 9), (46, 1, 5, 10);

                -- Seats för Salong 2
                -- Rad 1 (6 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (47, 2, 1, 1), (48, 2, 1, 2), (49, 2, 1, 3),
                (50, 2, 1, 4), (51, 2, 1, 5), (52, 2, 1, 6);

                -- Rad 2 (6 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (53, 2, 2, 1), (54, 2, 2, 2), (55, 2, 2, 3),
                (56, 2, 2, 4), (57, 2, 2, 5), (58, 2, 2, 6);

                -- Rad 3 (8 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (59, 2, 3, 1), (60, 2, 3, 2), (61, 2, 3, 3), (62, 2, 3, 4),
                (63, 2, 3, 5), (64, 2, 3, 6), (65, 2, 3, 7), (66, 2, 3, 8);

                -- Rad 4 (8 säten)
                INSERT INTO Seats (id, salong_id, row_num, seat_number) VALUES
                (67, 2, 4, 1), (68, 2, 4, 2), (69, 2, 4, 3), (70, 2, 4, 4),
                (71, 2, 4, 5), (72, 2, 4, 6), (73, 2, 4, 7), (74, 2, 4, 8);

                -- Ticket_Type
                INSERT INTO Ticket_Type (id, name, price) VALUES
                (1, 'Vuxen', 140),
                (2, 'Barn', 90),
                (3, 'Senior', 100);

                -- Showings (varje film visas i båda salongerna)
                INSERT INTO Showings (id, film_id, salong_id, start_time) VALUES
                (1, 1, 1, '2026-02-05 18:00:00'),
                (2, 1, 2, '2026-02-05 21:00:00'),
                (3, 2, 1, '2026-02-05 20:30:00'),
                (4, 2, 2, '2026-02-06 18:00:00'),
                (5, 3, 1, '2026-02-06 19:00:00'),
                (6, 3, 2, '2026-02-06 21:30:00'),
                (7, 4, 1, '2026-02-07 18:00:00'),
                (8, 4, 2, '2026-02-07 20:00:00'),
                (9, 5, 1, '2026-02-07 21:00:00'),
                (10, 5, 2, '2026-02-08 18:00:00'),
                (11, 6, 1, '2026-02-08 19:30:00'),
                (12, 6, 2, '2026-02-08 21:00:00');
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
