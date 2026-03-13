namespace WebApp;
public static class RestApi
{
    public static void Start()
    {

        App.MapPost("/api/{table}", async (
            HttpContext context, string table, JsonElement bodyJson
        ) =>
        {
            if (table == "bookings")
            {
                return await PostBooking(context, bodyJson);
            }
            var body = JSON.Parse(bodyJson.ToString());
            body.Delete("id");
            var parsed = ReqBodyParse(table, body);
            var columns = parsed.insertColumns;
            var values = parsed.insertValues;
            var sql = $"INSERT INTO {table}({columns}) VALUES({values})";
            var result = SQLQueryOne(sql, parsed.body, context);
            if (!result.HasKey("error"))
            {
                // Get the insert id and add to our result
                result.insertId = SQLQueryOne(
                    @$"SELECT id AS __insertId 
                       FROM {table} ORDER BY id DESC LIMIT 1"
                ).__insertId;
            }
            return RestResult.Parse(context, result);
        });

        App.MapGet("/api/{table}", (
            HttpContext context, string table
        ) =>
        {
            var query = RestQuery.Parse(context.Request.Query);
            if (query.error != null)
            {
                return RestResult.Parse(context, Arr(Obj(new { error = query.error })));
            }
            var sql = $"SELECT * FROM {table}" + query.sql;
            return RestResult.Parse(context, SQLQuery(sql, query.parameters, context));
        });

        App.MapGet("/api/{table}/{id}", (
            HttpContext context, string table, string id
        ) =>
            RestResult.Parse(context, SQLQueryOne(
                $"SELECT * FROM {table} WHERE id = @id",
                ReqBodyParse(table, Obj(new { id })).body,
                context
            ))
        );

        App.MapPut("/api/{table}/{id}", (
            HttpContext context, string table, string id, JsonElement bodyJson
        ) =>
        {
            var body = JSON.Parse(bodyJson.ToString());
            body.id = id;
            var parsed = ReqBodyParse(table, body);
            var update = parsed.update;
            var sql = $"UPDATE {table} SET {update} WHERE id = @id";
            var result = SQLQueryOne(sql, parsed.body, context);
            return RestResult.Parse(context, result);
        });

        App.MapDelete("/api/{table}/{id}", (
            HttpContext context, string table, string id
        ) => {
            if (table == "bookings")
            {
                RestResult.Parse(context, SQLQueryOne(
                    "CALL DeleteBooking(@id)",
                    Obj(new { id }),
                    context
                ));
            }
            else
            {
                RestResult.Parse(context, SQLQueryOne(
                    $"DELETE FROM {table} WHERE id = @id",
                    ReqBodyParse(table, Obj(new { id })).body,
                    context
                ));
            }
        });
    }
    public static async Task<IResult> PostBooking(HttpContext context, JsonElement bodyJson)
    {
        var body = JSON.Parse(bodyJson.ToString());

        var email = (string)body.email;
        var showingId = (int)body.showing_id;
        var seatsJson = JSON.Stringify(body.tickets);

        // Try to insert booking with a unique reference, retry on duplicate key
        const int maxRetries = 10;
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var bookingReference = BookingReferenceGenerator.Generate();
            var result = SQLQueryOne(
                "CALL CreateBookingWithSeats(@email, @showingId, @seatsJson, @bookingReference)",
                new { email, showingId, seatsJson, bookingReference },
                context
            );

            if (result.HasKey("error"))
            {
                string error = (string)result.error;
                // MySQL error 1062: Duplicate entry — retry with a new reference
                if (error.Contains("Duplicate entry") && attempt < maxRetries - 1)
                {
                    continue;
                }
                // Other error or last attempt — return the error
                return RestResult.Parse(context, result);
            }

            // Success — release locks and broadcast updated availability via SSE
            var holderId = Session.GetSessionId(context);
            SeatLockManager.ReleaseLocks(holderId, showingId);
            var unavailable = SeatLockManager.GetUnavailableSeatIds(showingId);
            await SseManager.BroadcastToShowing(showingId, unavailable);

            // Best-effort: send booking confirmation email without blocking booking success
            TrySendBookingConfirmationEmail(result, context);
            return RestResult.Parse(context, result);
        }

        return RestResult.Parse(context, Obj(new
        {
            error = "Unable to generate unique booking reference. Please try again."
        }));
    }

    private static void TrySendBookingConfirmationEmail(dynamic bookingInsertResult, HttpContext context)
    {
        try
        {
            if (bookingInsertResult == null || !bookingInsertResult.HasKey("booking_reference"))
            {
                return;
            }

            var bookingReference = (string)bookingInsertResult.booking_reference;
            var bookingDetails = SQLQueryOne(
                @"SELECT booking_reference, email, film_title, start_time, salong_name, seats
                  FROM booking_details
                  WHERE booking_reference = @bookingReference
                  LIMIT 1",
                new { bookingReference },
                context
            );

            if (bookingDetails == null)
            {
                return;
            }

            string emailError;
            if (!EmailService.TrySendBookingConfirmation(bookingDetails, out emailError) && !string.IsNullOrWhiteSpace(emailError))
            {
                Log("Booking confirmation email failed:", emailError);
            }
        }
        catch (Exception ex)
        {
            Log("Booking confirmation email failed:", ex.Message);
        }
    }
}