using System.Collections.Frozen;
using System.Text;

namespace KOD.Application.Common.Text;

/// <summary>
/// Builds URL-friendly identifiers from Ukrainian text using the official transliteration
/// (Resolution of the Cabinet of Ministers of Ukraine No. 55 of 27.01.2010).
/// </summary>
public static class Slug
{
    /// <summary>
    /// Transliteration table for letters in a non-initial position.
    /// </summary>
    private static readonly FrozenDictionary<char, string> Letters = new Dictionary<char, string>
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "h", ['ґ'] = "g", ['д'] = "d", ['е'] = "e", ['є'] = "ie",
        ['ж'] = "zh", ['з'] = "z", ['и'] = "y", ['і'] = "i", ['ї'] = "i", ['й'] = "i", ['к'] = "k", ['л'] = "l",
        ['м'] = "m", ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
        ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "shch", ['ю'] = "iu", ['я'] = "ia",
    }.ToFrozenDictionary();

    /// <summary>
    /// Transliteration of letters at the beginning of a word.
    /// </summary>
    private static readonly FrozenDictionary<char, string> InitialLetters = new Dictionary<char, string>
    {
        ['є'] = "ye", ['ї'] = "yi", ['й'] = "y", ['ю'] = "yu", ['я'] = "ya",
    }.ToFrozenDictionary();

    /// <summary>
    /// Creates a slug of at most <paramref name="maxLength"/> characters.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The slug, never empty.</returns>
    public static string From(string text, int maxLength = 80)
    {
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder(text.Length);
        var wordStart = true;
        foreach (var raw in text.ToLowerInvariant())
        {
            if (raw is '\'' or '’' or 'ʼ' or '`')
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(raw))
            {
                builder.Append(raw);
                wordStart = false;
            }
            else if ((wordStart && InitialLetters.TryGetValue(raw, out var initial)) || Letters.TryGetValue(raw, out initial))
            {
                builder.Append(initial);
                wordStart = false;
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
                wordStart = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (slug.Length > maxLength)
        {
            slug = slug[..maxLength].TrimEnd('-');
        }

        return slug.Length == 0 ? "item" : slug;
    }
}
