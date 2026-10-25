namespace KOD.Web.Components.Shared;

/// <summary>
/// Parsing helpers for query string values bound with <c>[SupplyParameterFromQuery]</c>.
/// </summary>
public static class QueryValues
{
    /// <summary>
    /// Parses an enum value by name (case-insensitive); undefined values yield <see langword="null"/>.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The raw value.</param>
    /// <returns>The parsed value or <see langword="null"/>.</returns>
    public static TEnum? Enum<TEnum>(string? value)
        where TEnum : struct, System.Enum
        => System.Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) && System.Enum.IsDefined(parsed) ? parsed : null;
}
