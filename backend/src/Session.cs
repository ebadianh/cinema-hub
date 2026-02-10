namespace WebApp;

public static partial class Session
{
    private static dynamic GetRawSession(HttpContext context)
    {
        // If the session is already cached in context.Items
        // (if we call this method more than once per request)
        var inContext = context.Items["session"];
        if (inContext != null) { return inContext; }

        // Get the cookie value if we have a session cookie
        // - otherwise create a session cookie
        context.Request.Cookies.TryGetValue("session", out string cookieValue);
        if (cookieValue == null)
        {
            cookieValue = Guid.NewGuid().ToString();
            context.Response.Cookies.Append("session", cookieValue);
        }

        // Get the session data from the database if it stored there
        // otherwise store it in the database
        var session = SQLQueryOne(
          "SELECT * FROM sessions WHERE id = @id",
          new { id = cookieValue }
        );
        if (session == null)
        {
            SQLQuery(
                "INSERT INTO sessions(id, data) VALUES(@id, @data)",
                new { id = cookieValue, data = "{}" }
            );
            session = Obj(new { id = cookieValue, data = Obj() });
        }

        // Cache the session in context.Items
        context.Items["session"] = session;
        return session;
    }

    public static void Start()
    {
        // Start a loop that delete old sessions continuously
        DeleteOldSessions();
    }

    public static dynamic Get(HttpContext context, string key)
    {
        var session = GetRawSession(context);

        // data can be either a string (from DB) or an Obj (newly created session)
        var data = session.data;

        // If it's an Obj (newly created session), it's empty
        if (data is not string)
        {
            return null;
        }

        string dataStr = (string)data;

        if (string.IsNullOrEmpty(dataStr) || dataStr == "{}")
        {
            return null;
        }

        // Parse using System.Text.Json to avoid circular references
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(dataStr);

        if (!dict.ContainsKey(key))
        {
            return null;
        }

        // Convert JsonElement to Obj for compatibility with rest of code
        return ConvertJsonElementToObj(dict[key]);
    }

    private static dynamic ConvertJsonElementToObj(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                var obj = Obj();
                foreach (var prop in element.EnumerateObject())
                {
                    obj[prop.Name] = ConvertJsonElementToObj(prop.Value);
                }
                return obj;
            case System.Text.Json.JsonValueKind.Array:
                return Arr(element.EnumerateArray().Select(ConvertJsonElementToObj).ToArray());
            case System.Text.Json.JsonValueKind.String:
                return element.GetString();
            case System.Text.Json.JsonValueKind.Number:
                if (element.TryGetInt32(out int intVal))
                    return intVal;
                if (element.TryGetInt64(out long longVal))
                    return longVal;
                return element.GetDouble();
            case System.Text.Json.JsonValueKind.True:
                return true;
            case System.Text.Json.JsonValueKind.False:
                return false;
            case System.Text.Json.JsonValueKind.Null:
                return null;
            default:
                return null;
        }
    }

    public static void Set(HttpContext context, string key, object value)
    {
        var session = GetRawSession(context);

        // Read existing data as JSON string
        string existingDataJson = session.data is string s ? s : "{}";

        // Parse to Dictionary using System.Text.Json (to avoid circular refs)
        var dict = string.IsNullOrEmpty(existingDataJson) || existingDataJson == "{}"
            ? new Dictionary<string, object>()
            : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(existingDataJson);

        // Convert value to plain object if it's an Obj (to avoid circular refs)
        object valueToSave = value;
        if (value != null && value.GetType().FullName == "Dyndata.Obj")
        {
            // Convert Obj to Dictionary by serializing and deserializing
            var tempJson = JSON.Stringify(value);
            valueToSave = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tempJson);
        }

        // Set the value
        dict[key] = valueToSave;

        // Serialize using System.Text.Json with ReferenceHandler
        var options = new System.Text.Json.JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };
        var jsonString = System.Text.Json.JsonSerializer.Serialize(dict, options);

        // Update in-memory cache
        session.data = jsonString;

        SQLQuery(
            @"UPDATE sessions
              SET modified = NOW(), data = @data
              WHERE id = @id",
            new
            {
                session.id,
                data = jsonString
            }
       );
    }
}