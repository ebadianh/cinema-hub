namespace WebApp;
public static class LoginRoutes
{
    private static Obj GetUser(HttpContext context)
    {
        return Session.Get(context, "user");
    }

    public static void Start()
    {
        App.MapPost("/api/login", (HttpContext context, JsonElement bodyJson) =>
        {
            var user = GetUser(context);
            var body = JSON.Parse(bodyJson.ToString());

            // If there is a user logged in already
            if (user != null)
            {
                var already = new { error = "A user is already logged in." };
                return RestResult.Parse(context, already);
            }

            // Find the user in the DB
            var dbUser = SQLQueryOne(
                "SELECT * FROM Users WHERE email = @email",
                new { body.email }
            );
            if (dbUser == null)
            {
                return RestResult.Parse(context, new { error = "No such user." });
            }
            if (dbUser.HasKey("error"))
            {
                return RestResult.Parse(context, dbUser);
            }

            // If the password doesn't match
            if (!Password.Verify(
                (string)body.password,
                (string)dbUser.password
            ))
            {
                return RestResult.Parse(context,
                    new { error = "Password mismatch." });
            }

            // Add the user to the session, without password
            dbUser.Delete("password");
            Session.Set(context, "user", dbUser);

            // Return the user
            return RestResult.Parse(context, dbUser!);
        });

        App.MapGet("/api/login", (Func<HttpContext, IResult>)(context =>
        {
            var user = GetUser(context);
            var payload = user != null ? user : Obj(new { error = "No user is logged in." });
            return Results.Text(
                JSON.Stringify(payload),
                "application/json; charset=utf-8",
                null,
                200
            );
        }));

        App.MapDelete("/api/login", (HttpContext context) =>
        {
            var user = GetUser(context);

            // Delete the user from the session
            Session.Set(context, "user", null);

            return RestResult.Parse(context, user == null ?
                new { error = "No user is logged in." } :
                new { status = "Successful logout." }
            );
        });
    }
}