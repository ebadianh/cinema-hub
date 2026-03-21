namespace WebApp;

public static class Acl
{
    private static Arr rules = Arr();

    private static void EnsureMinimumRules()
    {
        SQLQuery(@"
            INSERT IGNORE INTO acl (userRoles, method, allow, route, `match`, comment) VALUES
            ('visitor,user,admin', 'GET',  'allow', '/api/films', 'true', 'Allow all to read films'),
            ('visitor,user,admin', 'GET',  'allow', '/api/films/{id}', 'true', 'Allow all to read single film'),
            ('visitor,user,admin', 'GET',  'allow', '/api/showings', 'true', 'Allow all to read showings'),
            ('visitor,user,admin', 'GET',  'allow', '/api/showings/{id}', 'true', 'Allow all to read single showing'),
            ('visitor,user,admin', 'GET',  'allow', '/api/showings_detail', 'true', 'Allow all to read showing details view'),
            ('visitor,user,admin', 'GET',  'allow', '/api/showings_detail/{id}', 'true', 'Allow all to read single showing detail'),
            ('visitor,user,admin', 'GET',  'allow', '/api/seats', 'true', 'Allow all to read seats'),
            ('visitor,user,admin', 'GET',  'allow', '/api/ticket_type', 'true', 'Allow all to read ticket types'),
            ('visitor,user,admin', 'GET',  'allow', '/api/directors', 'true', 'Allow all to read directors'),
            ('visitor,user,admin', 'GET',  'allow', '/api/actors', 'true', 'Allow all to read actors'),
            ('visitor,user,admin', 'POST', 'allow', '/api/bookings', 'true', 'Allow all to create bookings'),
            ('visitor,user,admin', 'POST', 'allow', '/api/showings/{showingId}/seats/lock', 'true', 'Allow all to lock seats'),
            ('visitor,user,admin', 'POST', 'allow', '/api/showings/{showingId}/seats/release', 'true', 'Allow all to release seats'),
            ('visitor,user,admin', 'GET',  'allow', '/api/showings/{showingId}/seats/stream', 'true', 'Allow all to stream seat locks')
        ");
    }

    public static async void Start()
    {
        EnsureMinimumRules();

        // Read rules from db once a minute
        while (true)
        {
            EnsureMinimumRules();
            UnpackRules(SQLQuery("SELECT * FROM acl ORDER BY allow"));
            await Task.Delay(60000);
        }
    }

    public static void UnpackRules(Arr allRules)
    {
        if (allRules.Length == 0 || allRules[0].error != null) { return; }

        rules = allRules.Map(x => new
        {
            ___ = x,
            regexPattern = BuildRegexPattern((string)x.route),
            userRoles = ((Arr)Arr(x.userRoles.Split(','))).Map(role => role.Trim())
        });

        // TEMP DEBUG - remove when everything works
   /*      Console.WriteLine("=== ACL RULES LOADED ===");
        foreach (var rule in rules)
        {
            Console.WriteLine($"{rule.id} | {rule.method} | {rule.route} | {rule.regexPattern}");
        } */
    }

    private static string BuildRegexPattern(string route)
    {
        var parts = route.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var regexParts = parts.Select(part =>
        {
            // Route params like {id}, {showingId}
            if (part.StartsWith("{") && part.EndsWith("}"))
            {
                return @"[^/]+";
            }

            // Wildcard support
            if (part == "*")
            {
                return @".*";
            }

            // Normal literal segment
            return Regex.Escape(part);
        });

        return "^/" + string.Join("/", regexParts) + @"/?$";
    }

    public static bool Allow(
        HttpContext context, string method = "", string path = ""
    )
    {
        // Return true/allowed for everything if acl is off in Globals
        if (!Globals.aclOn) { return true; }

        // Get info about the requested route and logged in user
        method = method != "" ? method : context.Request.Method;
        path = path != "" ? path : context.Request.Path.ToString();

        var user = Session.Get(context, "user");
        var userRole = user == null ? "visitor" : (string)user.role;
        var userEmail = user == null ? "" : (string)user.email;

        // Go through all acl rules and set allowed accordingly
        var allowed = false;
        Obj appliedAllowRule = null;
        Obj appliedDisallowRule = null;

        foreach (var rule in rules)
        {
            var ruleMethod = (string)rule.method;
            var ruleRegexPattern = (string)rule.regexPattern;
            var ruleRoles = (Arr)rule.userRoles;
            var ruleMatch = ((string)rule.match) == "true";
            var ruleAllow = ((string)rule.allow) == "allow";
            var ruleRoute = (string)rule.route;

            var roleOk = ruleRoles.Includes(userRole);
            var methodOk = method == ruleMethod || ruleMethod == "*";
            var pathOk = Regex.IsMatch(path, ruleRegexPattern);

            // If match is false, negate pathOk
            pathOk = ruleMatch ? pathOk : !pathOk;

            var allOk = roleOk && methodOk && pathOk;

            // TEMP DEBUG - remove when everything works
            if (
                path.StartsWith("/api/films/") ||
                path.StartsWith("/api/users/") ||
                path.StartsWith("/api/showings/")
            )
            {
                Console.WriteLine(
                    $"ACL CHECK | path={path} | method={method} | " +
                    $"rule={ruleRoute} | regex={ruleRegexPattern} | " +
                    $"roleOk={roleOk} methodOk={methodOk} pathOk={pathOk} allOk={allOk}"
                );
            }

            // Whitelist first, blacklist on top
            var oldAllowed = allowed;
            allowed = ruleAllow ? allowed || allOk : allOk ? false : allowed;

            if (oldAllowed != allowed)
            {
                if (ruleAllow) { appliedAllowRule = rule; }
                else { appliedDisallowRule = rule; }
            }
        }

        // Collect info for debug log
        var toLog = Obj(new { userRole, userEmail, aclAllowed = allowed });

        if (Globals.detailedAclDebug && appliedAllowRule != null)
        {
            toLog.aclAppliedAllowRule = appliedAllowRule;
        }

        if (Globals.detailedAclDebug && appliedDisallowRule != null)
        {
            toLog.aclAppliedDisallowRule = appliedDisallowRule;
        }

        if (userEmail == "") { toLog.Delete("userEmail"); }

        DebugLog.Add(context, toLog);

        return allowed;
    }
}