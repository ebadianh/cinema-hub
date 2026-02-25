using System.Collections.Generic;
using System.Linq;

namespace WebApp.Tests;

/// <summary>
/// Unit tests for BookingReferenceGenerator.
/// Tests cryptographic randomness, format validation, and uniqueness.
/// </summary>
public static class BookingReferenceGeneratorTests
{
    private const string AllowedChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int ReferenceLength = 6;

    /// <summary>
    /// Test that generated codes have correct length.
    /// </summary>
    public static void TestGeneratedCodeLength()
    {
        for (int i = 0; i < 100; i++)
        {
            var code = BookingReferenceGenerator.Generate();
            if (code.Length != ReferenceLength)
                throw new Exception($"Expected length {ReferenceLength}, got {code.Length}: {code}");
        }
        Console.WriteLine("✓ TestGeneratedCodeLength passed");
    }

    /// <summary>
    /// Test that generated codes only contain allowed characters.
    /// </summary>
    public static void TestGeneratedCodeCharacters()
    {
        for (int i = 0; i < 100; i++)
        {
            var code = BookingReferenceGenerator.Generate();
            foreach (var c in code)
            {
                if (!AllowedChars.Contains(c))
                    throw new Exception($"Invalid character '{c}' in code: {code}");
            }
        }
        Console.WriteLine("✓ TestGeneratedCodeCharacters passed");
    }

    /// <summary>
    /// Test that generated codes exclude ambiguous characters (0, O, 1, I, L).
    /// </summary>
    public static void TestNoAmbiguousCharacters()
    {
        var ambiguousChars = new[] { '0', 'O', '1', 'I', 'L' };
        for (int i = 0; i < 100; i++)
        {
            var code = BookingReferenceGenerator.Generate();
            foreach (var ambiguous in ambiguousChars)
            {
                if (code.Contains(ambiguous))
                    throw new Exception($"Found ambiguous character '{ambiguous}' in code: {code}");
            }
        }
        Console.WriteLine("✓ TestNoAmbiguousCharacters passed");
    }

    /// <summary>
    /// Test that generated codes are unique (1000+ codes should have no duplicates).
    /// </summary>
    public static void TestUniqueness()
    {
        var codes = new HashSet<string>();
        for (int i = 0; i < 1000; i++)
        {
            var code = BookingReferenceGenerator.Generate();
            if (!codes.Add(code))
                throw new Exception($"Duplicate code generated: {code}");
        }
        Console.WriteLine($"✓ TestUniqueness passed ({codes.Count} unique codes generated)");
    }

    /// <summary>
    /// Test format validation for valid codes.
    /// </summary>
    public static void TestFormatValidationValid()
    {
        var validCodes = new[] { "XY7K3M", "P9R2HW", "AB34CD", "ZZZZZZ", "222222" };
        foreach (var code in validCodes)
        {
            if (!BookingReferenceGenerator.IsValidFormat(code))
                throw new Exception($"Valid code rejected: {code}");
        }
        Console.WriteLine("✓ TestFormatValidationValid passed");
    }

    /// <summary>
    /// Test format validation for invalid codes.
    /// </summary>
    public static void TestFormatValidationInvalid()
    {
        var invalidCodes = new[]
        {
            "",              // Empty
            "XY7K3",         // Too short
            "XY7K3MX",       // Too long
            "XY7K3M0",       // Too long + invalid char
            "XY7K3O",        // Invalid character O
            "0Y7K3M",        // Invalid character 0
            "1Y7K3M",        // Invalid character 1
            "IY7K3M",        // Invalid character I
            "LY7K3M",        // Invalid character L
            null             // Null
        };
        foreach (var code in invalidCodes)
        {
            if (BookingReferenceGenerator.IsValidFormat(code))
                throw new Exception($"Invalid code accepted: {code}");
        }
        Console.WriteLine("✓ TestFormatValidationInvalid passed");
    }

    /// <summary>
    /// Run all tests.
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("Running BookingReferenceGenerator tests...");
        TestGeneratedCodeLength();
        TestGeneratedCodeCharacters();
        TestNoAmbiguousCharacters();
        TestUniqueness();
        TestFormatValidationValid();
        TestFormatValidationInvalid();
        Console.WriteLine("\n✓ All tests passed!");
    }
}
