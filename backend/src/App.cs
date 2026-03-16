// #region agent log
{
    var log = $"{{\"sessionId\":\"f56b18\",\"runId\":\"pre-fix\",\"hypothesisId\":\"H1\",\"location\":\"App.cs:2\",\"message\":\"Main args\",\"data\":{{\"argsLength\":{args.Length}}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
    System.IO.File.AppendAllText("C:\\Users\\Mo\\repos\\cinema-hub\\debug-f56b18.log", log);
}
// #endregion

var port = args.Length > 0 ? args[0] : "5000";
var frontendPath = args.Length > 1 ? args[1] : "../frontend";

// #region agent log
{
    var log = $"{{\"sessionId\":\"f56b18\",\"runId\":\"post-fix\",\"hypothesisId\":\"H2\",\"location\":\"App.cs:9\",\"message\":\"Resolved settings\",\"data\":{{\"port\":\"{port}\",\"frontendPath\":\"{frontendPath}\"}},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
    System.IO.File.AppendAllText("C:\\Users\\Mo\\repos\\cinema-hub\\debug-f56b18.log", log);
}
// #endregion

// Global settings
Globals = Obj(new
{
    debugOn = true,
    detailedAclDebug = false,
    aclOn = false,
    isSpa = true,
    port = port,
    serverName = "Minimal API Backend",
    frontendPath = frontendPath,
    sessionLifeTimeHours = 2
});

Server.Start();