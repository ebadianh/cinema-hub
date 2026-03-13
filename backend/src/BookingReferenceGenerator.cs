using System.Security.Cryptography;

namespace WebApp;

public static class BookingReferenceGenerator
{
    private const string AllowedChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int ReferenceLength = 6;

    /// <summary>
    /// Generates a cryptographically secure 6-character booking reference code.
    /// Uses only unambiguous characters (excludes 0, O, 1, I, L).
    /// Example: XY7K3M
    /// </summary>
    public static string Generate()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[ReferenceLength];
        var chars = new char[ReferenceLength];

        rng.GetBytes(bytes);

        for (int i = 0; i < ReferenceLength; i++)
        {
            chars[i] = AllowedChars[bytes[i] % AllowedChars.Length];
        }

        return new string(chars);
    }

    /// <summary>
    /// Validates if a string matches the expected booking reference format.
    /// </summary>
    public static bool IsValidFormat(string reference)
    {
        if (string.IsNullOrEmpty(reference) || reference.Length != ReferenceLength)
            return false;
        return reference.All(c => AllowedChars.Contains(c));
    }
}
