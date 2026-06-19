namespace MediBook.Extensions;

public static class StringExtensions
{
    /// <summary>Returns <paramref name="fallback"/> when the value is null, empty, or whitespace.</summary>
    public static string IfEmpty(this string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    /// <summary>Trims and lowercases for case-insensitive comparison.</summary>
    public static string Normalized(this string value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
