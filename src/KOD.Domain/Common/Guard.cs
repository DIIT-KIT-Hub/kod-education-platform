namespace KOD.Domain.Common;

/// <summary>
/// Small set of invariant checks that throw <see cref="DomainException"/> with user-facing messages.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Ensures that <paramref name="value"/> is not empty and does not exceed <paramref name="maxLength"/>.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="fieldName">The user-facing name of the field.</param>
    /// <returns>The trimmed value.</returns>
    public static string Text(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException($"Поле «{fieldName}» є обов'язковим.");
        }

        return trimmed.Length > maxLength
            ? throw new DomainException($"Поле «{fieldName}» не може бути довшим за {maxLength} символів.")
            : trimmed;
    }

    /// <summary>
    /// Normalizes an optional text value: trims it, converts blanks to <see langword="null"/> and checks the length.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="fieldName">The user-facing name of the field.</param>
    /// <returns>The trimmed value or <see langword="null"/>.</returns>
    public static string? OptionalText(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > maxLength
            ? throw new DomainException($"Поле «{fieldName}» не може бути довшим за {maxLength} символів.")
            : trimmed;
    }
}
