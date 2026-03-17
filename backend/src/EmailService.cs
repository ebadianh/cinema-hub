using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;
using System.Text;

namespace WebApp;

public static class EmailService
{
    public static bool TrySendBookingConfirmation(dynamic booking, out string error)
    {
        error = "";

        try
        {
            SendBookingConfirmation(booking);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void SendBookingConfirmation(dynamic booking)
    {
        if (booking == null)
        {
            throw new InvalidOperationException("Bokningsdetaljer saknas.");
        }

        var to = (string)booking.email;
        var bookingReference = (string)booking.booking_reference;
        var filmTitle = booking.film_title != null ? (string)booking.film_title : "Okand film";
        var salongName = booking.salong_name != null ? (string)booking.salong_name : "Okand salong";
        var startTime = booking.start_time != null ? (string)booking.start_time : "";

        var showtimeText = startTime;
        if (DateTime.TryParse(startTime, out var parsed))
        {
                        showtimeText = parsed.ToString("dddd d MMMM yyyy, HH:mm", CultureInfo.GetCultureInfo("sv-SE"));
        }

                var seatData = BuildSeatRowsAndTotal(booking.seats);
                var subject = $"Bokningsbekräftelse | {bookingReference}";
                var totalPriceText = seatData.Item2.ToString("0.##", CultureInfo.GetCultureInfo("sv-SE"));
                var body = $@"
                <!doctype html>
                <html lang=""sv"">
                <head>
                    <meta charset=""utf-8"" />
                    <meta name=""viewport"" content=""width=device-width,initial-scale=1"" />
                    <title>Bokningsbekräftelse</title>
                </head>
                <body style=""margin:0;padding:0;background-color:#f2f4f8;font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;"">
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background-color:#f2f4f8;padding:24px 12px;"">
                <tr>
                <td align=""center"">
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""max-width:640px;background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e5e7eb;"">
                    <tr>
                        <td style=""padding:26px 28px;background:#0f172a;color:#ffffff;"">
                            <div style=""font-size:13px;letter-spacing:0.08em;text-transform:uppercase;color:#cbd5e1;"">Cinema Hub</div>
                            <h1 style=""margin:8px 0 6px 0;font-size:26px;line-height:1.2;font-weight:700;"">Tack för din bokning</h1>
                            <p style=""margin:0;font-size:15px;line-height:1.5;color:#e2e8f0;"">Din bokning är bekräftad. Här är allt du behöver inför besöket.</p>
                        </td>
                    </tr>

                    <tr>
                        <td style=""padding:24px 28px 8px 28px;"">
                            <div style=""display:inline-block;padding:10px 14px;border-radius:10px;background:#eff6ff;border:1px solid #bfdbfe;"">
                                <div style=""font-size:12px;color:#334155;text-transform:uppercase;letter-spacing:0.06em;"">Bokningsnummer</div>
                                <div style=""font-size:22px;font-weight:700;color:#0f172a;letter-spacing:0.04em;"">{WebUtility.HtmlEncode(bookingReference)}</div>
                            </div>
                        </td>
                    </tr>

                    <tr>
                        <td style=""padding:8px 28px 0 28px;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""border-collapse:collapse;"">
                                <tr>
                                    <td style=""padding:6px 0;color:#64748b;font-size:14px;"">Film</td>
                                    <td style=""padding:6px 0;color:#0f172a;font-size:14px;font-weight:600;text-align:right;"">{WebUtility.HtmlEncode(filmTitle)}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:6px 0;color:#64748b;font-size:14px;"">Visningstid</td>
                                    <td style=""padding:6px 0;color:#0f172a;font-size:14px;font-weight:600;text-align:right;"">{WebUtility.HtmlEncode(showtimeText)}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:6px 0 12px 0;color:#64748b;font-size:14px;"">Salong</td>
                                    <td style=""padding:6px 0 12px 0;color:#0f172a;font-size:14px;font-weight:600;text-align:right;"">{WebUtility.HtmlEncode(salongName)}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <tr>
                        <td style=""padding:8px 28px 0 28px;"">
                            <h2 style=""margin:0 0 10px 0;font-size:17px;color:#0f172a;"">Biljetter och platser</h2>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""border-collapse:collapse;border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;"">
                                <tr style=""background:#f8fafc;"">
                                    <th align=""left"" style=""padding:10px 12px;font-size:12px;color:#475569;text-transform:uppercase;letter-spacing:0.05em;"">Plats</th>
                                    <th align=""left"" style=""padding:10px 12px;font-size:12px;color:#475569;text-transform:uppercase;letter-spacing:0.05em;"">Biljettyp</th>
                                    <th align=""right"" style=""padding:10px 12px;font-size:12px;color:#475569;text-transform:uppercase;letter-spacing:0.05em;"">Pris</th>
                                </tr>
                                {seatData.Item1}
                            </table>
                        </td>
                    </tr>

                    <tr>
                        <td style=""padding:16px 28px 0 28px;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""border-collapse:collapse;"">
                                <tr>
                                    <td style=""padding:6px 0;color:#64748b;font-size:14px;"">Antal biljetter</td>
                                    <td style=""padding:6px 0;color:#0f172a;font-size:14px;font-weight:600;text-align:right;"">{seatData.Item3}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:6px 0 0 0;color:#0f172a;font-size:15px;font-weight:700;"">Totalt</td>
                                    <td style=""padding:6px 0 0 0;color:#0f172a;font-size:20px;font-weight:700;text-align:right;"">{WebUtility.HtmlEncode(totalPriceText)} kr</td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <tr>
                        <td style=""padding:22px 28px 26px 28px;"">
                            <p style=""margin:0;color:#475569;font-size:14px;line-height:1.6;"">Spara detta mejl så att du enkelt hittar ditt bokningsnummer vid behov.</p>
                        </td>
                    </tr>
                </table>
            </td>
            </tr>
        </table>
        </body>
        </html>";

        SendEmail(to, subject, body);
    }

        private static (string RowsHtml, decimal TotalPrice, int SeatCount) BuildSeatRowsAndTotal(dynamic seats)
    {
        if (seats == null)
        {
                        return ("<tr><td colspan=\"3\" style=\"padding:12px;color:#64748b;font-size:14px;\">Inga platsdetaljer tillgängliga</td></tr>", 0m, 0);
        }

                var rowsHtml = new StringBuilder();
                decimal totalPrice = 0m;
                int seatCount = 0;

        foreach (var seat in seats)
        {
            var rowNum = seat.row_num;
            var seatNumber = seat.seat_number;
                        var ticketType = seat.ticket_type != null ? (string)seat.ticket_type : "Biljett";

                        decimal seatPrice = 0m;
                        decimal.TryParse(Convert.ToString(seat.ticket_price, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out seatPrice);
                        totalPrice += seatPrice;
                        seatCount++;

                        rowsHtml.Append($@"
                                <tr>
                                    <td style=""padding:10px 12px;border-top:1px solid #e2e8f0;color:#0f172a;font-size:14px;"">Rad {WebUtility.HtmlEncode(Convert.ToString(rowNum))}, Plats {WebUtility.HtmlEncode(Convert.ToString(seatNumber))}</td>
                                    <td style=""padding:10px 12px;border-top:1px solid #e2e8f0;color:#334155;font-size:14px;"">{WebUtility.HtmlEncode(ticketType)}</td>
                                    <td align=""right"" style=""padding:10px 12px;border-top:1px solid #e2e8f0;color:#0f172a;font-size:14px;font-weight:600;"">{WebUtility.HtmlEncode(seatPrice.ToString("0.##", CultureInfo.GetCultureInfo("sv-SE")))} kr</td>
                                </tr>");
        }

                if (seatCount == 0)
                {
                        return ("<tr><td colspan=\"3\" style=\"padding:12px;color:#64748b;font-size:14px;\">Inga platsdetaljer tillgängliga</td></tr>", 0m, 0);
                }

                return (rowsHtml.ToString(), totalPrice, seatCount);
    }

    public static void SendEmail(string to, string subject, string body)
    {
        var config = ReadEmailConfig();

        var message = new MimeMessage
        {
            Subject = subject,
            Body = new TextPart("html") { Text = body }
        };

        message.From.Add(MailboxAddress.Parse(config.EmailUsername));
        message.To.Add(MailboxAddress.Parse(to));

        using var client = new SmtpClient();
        client.Connect(config.SmtpServer, config.SmtpPort, SecureSocketOptions.StartTls);
        client.Authenticate(config.EmailUsername, config.EmailPassword);
        client.Send(message);
        client.Disconnect(true);
    }

    private static EmailConfig ReadEmailConfig()
    {
        var configPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "db-config.json"
        );

        var configJson = File.ReadAllText(configPath);
        var config = JSON.Parse(configJson);

        var smtpServer = config.HasKey("smtpServer") ? (string)config.smtpServer : "";
        var smtpPort = config.HasKey("smtpPort") ? Convert.ToInt32(config.smtpPort) : 0;
        var emailUsername = config.HasKey("emailUsername") ? (string)config.emailUsername : "";
        var emailPassword = config.HasKey("emailPassword") ? (string)config.emailPassword : "";

        if (string.IsNullOrWhiteSpace(smtpServer) ||
            smtpPort <= 0 ||
            string.IsNullOrWhiteSpace(emailUsername) ||
            string.IsNullOrWhiteSpace(emailPassword))
        {
            throw new InvalidOperationException("SMTP-installningar saknas i db-config.json.");
        }

        return new EmailConfig
        {
            SmtpServer = smtpServer,
            SmtpPort = smtpPort,
            EmailUsername = emailUsername,
            EmailPassword = emailPassword
        };
    }

    private sealed class EmailConfig
    {
        public string SmtpServer { get; init; } = "";
        public int SmtpPort { get; init; }
        public string EmailUsername { get; init; } = "";
        public string EmailPassword { get; init; } = "";
    }
}
