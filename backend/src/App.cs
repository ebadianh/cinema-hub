// Global settings
Globals = Obj(new
{
    debugOn = true,
    detailedAclDebug = true,
    aclOn = true,
    isSpa = true,
    port = args[0],
    serverName = "Minimal API Backend",
    frontendPath = args[1],
    sessionLifeTimeHours = 2
});

Server.Start();