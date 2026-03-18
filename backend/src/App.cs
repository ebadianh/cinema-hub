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