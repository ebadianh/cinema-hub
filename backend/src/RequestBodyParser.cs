namespace WebApp;
public static class RequestBodyParser
{
    public static dynamic ReqBodyParse(string table, Obj body)
    {
        // Always remove "role" for users table
        var keys = body.GetKeys().Filter(key => table != "users" || key != "role");
        // Clean up the body by converting strings to numbers when possible
        var cleaned = Obj();
        body.GetKeys().ForEach(key
            => cleaned[key] = ((object)(body[key])).TryToNumber());

        if (table == "users")
        {
            if (cleaned.HasKey("firstName") && cleaned.firstName is string)
            {
                string fn = cleaned.firstName;
                if (fn.Length > 0)
                    cleaned.firstName = char.ToUpper(fn[0]) + fn.Substring(1).ToLower();
            }
            if (cleaned.HasKey("lastName") && cleaned.lastName is string)
            {
                string ln = cleaned.lastName;
                if (ln.Length > 0)
                    cleaned.lastName = char.ToUpper(ln[0]) + ln.Substring(1).ToLower();
            }
        }
        // Always encrypt fields named "password"
        if (cleaned.HasKey("password"))
        {
            cleaned.password = Password.Encrypt(cleaned.password + "");
        }
        // Return parts to use when building the SQL query + the cleaned body
        return Obj(new
        {
            insertColumns = keys.Join(","),
            insertValues = "@" + keys.Join(",@"),
            update = keys.Filter(key => key != "id").Map(key => $"{key}=@{key}").Join(","),
            body = cleaned
        });
    }
}