namespace WebApp;

public static class AiSnackService
{
    public static dynamic GetSnackMenu()
    {
        var facts = AiConfigService.CinemaFacts;
        if (facts == null) return null;

        try
        {
            return facts.snacks;
        }
        catch
        {
            return null;
        }
    }

    public static dynamic FindSnackByName(string snackItem)
    {
        if (string.IsNullOrWhiteSpace(snackItem)) return null;

        var snacks = GetSnackMenu();
        if (snacks == null) return null;

        var needle = snackItem.Trim().ToLowerInvariant();

        foreach (var groupName in new[] { "classics", "drinks", "premium" })
        {
            try
            {
                var arr = snacks[groupName];
                foreach (var item in arr)
                {
                    try
                    {
                        var name = item.name != null ? item.name.ToString() : item.ToString();
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        var n = name.Trim().ToLowerInvariant();

                        if (n == needle || n.Contains(needle) || needle.Contains(n))
                            return item;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }

    public static bool IsKnownSnackCategory(string snackItem)
    {
        if (string.IsNullOrWhiteSpace(snackItem)) return false;

        var s = snackItem.Trim().ToLowerInvariant();

        return s.Contains("popcorn") ||
               s.Contains("godis") ||
               s.Contains("dryck") ||
               s.Contains("läsk") ||
               s.Contains("juice") ||
               s.Contains("kaffe") ||
               s.Contains("te") ||
               s.Contains("energidryck") ||
               s.Contains("nachos") ||
               s.Contains("glass") ||
               s.Contains("dessert");
    }

    public static bool LooksLikeSpecificBrandRequest(string snackItem)
{
    if (string.IsNullOrWhiteSpace(snackItem))
        return false;

    var s = snackItem.Trim().ToLowerInvariant();

    // brand / specific product signals
    return
        s.Contains("cola zero") ||
        s.Contains("pepsi max") ||
        s.Contains("coca cola") ||
        s.Contains("fanta") ||
        s.Contains("sprite") ||
        s.Contains("red bull") ||
        s.Contains("monster") ||
        s.Contains("loka") ||
        s.Contains("ramlösa");
}
}