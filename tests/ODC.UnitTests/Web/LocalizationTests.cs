using System.Globalization;
using System.Text.RegularExpressions;
using ODC.Web.Shared.Localization;

namespace ODC.UnitTests.Web;

/// <summary>
/// Checks that every interface text marked for translation has an English translation.
/// </summary>
public sealed partial class LocalizationTests
{
    /// <summary>
    /// Source folders scanned for interface texts.
    /// </summary>
    private static readonly string[] SourceProjects = ["ODC.Web"];

    static LocalizationTests()
    {
        Texts.Register(typeof(ODC.Web.Endpoints.IEndpoint).Assembly);
    }

    [Fact]
    public void Every_translated_server_text_has_an_english_translation()
    {
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in SourceFiles("*.cs"))
        {
            var source = File.ReadAllText(file);
            var texts = TranslatedLiteral().Matches(source)
                .Concat(ValidationMessageLiteral().Matches(source))
                .Concat(GuardFieldLiteral().Matches(source))
                .Select(static m => Unescape(m.Groups["text"].Value));
            foreach (var text in texts.Where(static t => !Texts.HasTranslation(t, Languages.English)))
            {
                missing.Add($"{Path.GetFileName(file)}: {text}");
            }

            // In("context", "…"): the contextual key or, failing that, the plain text.
            foreach (Match match in ContextualLiteral().Matches(source))
            {
                var text = Unescape(match.Groups["text"].Value);
                var context = match.Groups["context"].Value;
                if (!Texts.HasTranslation($"{context}|{text}", Languages.English) && !Texts.HasTranslation(text, Languages.English))
                {
                    missing.Add($"{Path.GetFileName(file)}: {context}|{text}");
                }
            }
        }

        Assert.True(missing.Count == 0, "No English translation for:\n" + string.Join('\n', missing));
    }

    [Fact]
    public void Texts_follow_the_current_ui_culture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
            Assert.Equal("The “Назва” field is required.".Replace("Назва", Texts.T("Назва"), StringComparison.Ordinal), Texts.T("Поле «{0}» є обов'язковим.", Texts.T("Назва")));

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("uk-UA");
            Assert.Equal("Поле «Назва» є обов'язковим.", Texts.T("Поле «{0}» є обов'язковим.", Texts.T("Назва")));
            Assert.Equal("Текст без перекладу", Texts.T("Текст без перекладу"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("en-GB", "en")]
    [InlineData("UK", "uk")]
    [InlineData("de", null)]
    [InlineData(" ", null)]
    public void Language_codes_are_normalized(string value, string? expected)
        => Assert.Equal(expected, Languages.Normalize(value));

    /// <summary>
    /// Finds the repository root (the folder with the solution file).
    /// </summary>
    /// <returns>The path.</returns>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrganizationalDocumentsCabinet.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }

    /// <summary>
    /// Lists the source files of the application projects (generated migrations excluded).
    /// </summary>
    /// <param name="pattern">The file name pattern.</param>
    /// <returns>The paths.</returns>
    private static IEnumerable<string> SourceFiles(string pattern)
        => SourceProjects
            .SelectMany(p => Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src", p), pattern, SearchOption.AllDirectories))
            .Where(static f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>
    /// Resolves the escapes of a C# string literal.
    /// </summary>
    /// <param name="literal">The literal content.</param>
    /// <returns>The text.</returns>
    private static string Unescape(string literal) => Regex.Unescape(literal);

    /// <summary>
    /// <c>T("…")</c> / <c>Texts.T("…")</c> (also with format arguments).
    /// </summary>
    [GeneratedRegex("""\bT\(\s*"(?<text>(?:[^"\\]|\\.)*)"\s*[,)]""")]
    private static partial Regex TranslatedLiteral();

    /// <summary>
    /// <c>In("context", "…")</c> / <c>Texts.In("context", "…")</c>.
    /// </summary>
    [GeneratedRegex("""\bIn\(\s*"(?<context>[^"]+)"\s*,\s*"(?<text>(?:[^"\\]|\\.)*)"\s*\)""")]
    private static partial Regex ContextualLiteral();

    /// <summary>
    /// <c>ErrorMessage = "…"</c> of data annotations (translated when shown).
    /// </summary>
    [GeneratedRegex("""\bErrorMessage\s*=\s*"(?<text>(?:[^"\\]|\\.)*)"(?=\s*[,)\]])""")]
    private static partial Regex ValidationMessageLiteral();

    /// <summary>
    /// The field name argument of <c>Guard.Text</c> / <c>Guard.OptionalText</c> (translated inside the guard).
    /// </summary>
    [GeneratedRegex("""(?:\bGuard\.(?:Optional)?Text\(|\bText\(value, )[^;]*?,\s*"(?<text>[^"]+)"\)""")]
    private static partial Regex GuardFieldLiteral();
}
