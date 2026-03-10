namespace WebApp;

public static class AiTitleNormalizationService
{
    public static string NormalizeTitleCandidate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        var s = input.Trim();

        // simple aliases
        if (s.Equals("gudfadern", StringComparison.OrdinalIgnoreCase))
            return "The Godfather";

        if (s.Equals("toy story", StringComparison.OrdinalIgnoreCase))
            return "Toy Story";

        return s;
    }

    public static string ExtractLikelyTitleFromPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return "";

        var s = prompt.Trim();

        var prefixes = new[]
        {
            "när går ",
            "när visas ",
            "går ",
            "finns ",
            "visa ",
            "visas ",
            "är "
        };

        var lower = s.ToLowerInvariant();

        for (int i = 0; i < prefixes.Length; i++)
        {
            if (lower.StartsWith(prefixes[i]))
            {
                s = s.Substring(prefixes[i].Length).Trim();
                break;
            }
        }

        s = s.Trim('?', '!', '.', ',', ' ');

        // remove trailing date-ish words that should be handled elsewhere
        var noise = new[]
        {
            "idag", "imorgon", "ikväll", "i helgen", "hos er", "på bio"
        };

        foreach (var n in noise)
        {
            if (s.EndsWith(" " + n, StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - n.Length).Trim();
            }
            else if (s.Equals(n, StringComparison.OrdinalIgnoreCase))
            {
                s = "";
            }
        }

        return NormalizeTitleCandidate(s);
    }
}