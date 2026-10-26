using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace KOD.Domain.Localization;

/// <summary>
/// Translates user-facing texts into the language of the current request (<see cref="CultureInfo.CurrentUICulture"/>).
/// <para>
/// The Ukrainian text itself is the key: <c>T("Зберегти")</c> returns «Зберегти» for Ukrainian and «Save» for English.
/// Translations are JSON objects (<c>{ "Зберегти": "Save" }</c>) embedded into each KOD assembly from
/// <c>Localization/&lt;language&gt;/*.json</c> (see <c>Directory.Build.props</c>); they are picked up automatically when the
/// assembly loads. A text without a translation falls back to Ukrainian.
/// </para>
/// </summary>
public static class Texts
{
    /// <summary>
    /// The manifest resource name prefix of the embedded translation files.
    /// </summary>
    internal const string ResourcePrefix = "kod-i18n:";

    /// <summary>
    /// The translations per language code (<c>en</c>), merged from every loaded assembly.
    /// </summary>
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> Catalogs = new(StringComparer.Ordinal);

    /// <summary>
    /// The assemblies whose translations were already loaded.
    /// </summary>
    private static readonly ConcurrentDictionary<string, bool> LoadedAssemblies = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes static members of the <see cref="Texts"/> class: loads the translations of the assemblies that are already
    /// loaded and subscribes to the ones that load later.
    /// </summary>
#pragma warning disable CA1810 // The event subscription must happen before the scan, so a static constructor is required.
    static Texts()
#pragma warning restore CA1810
    {
        AppDomain.CurrentDomain.AssemblyLoad += static (_, args) => Register(args.LoadedAssembly);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Register(assembly);
        }
    }

    /// <summary>
    /// Translates a text into the current UI language.
    /// </summary>
    /// <param name="text">The Ukrainian text.</param>
    /// <returns>The translation, or <paramref name="text"/> when the language is Ukrainian or no translation exists.</returns>
    public static string T(string text) => Translate(text, Languages.Current);

    /// <summary>
    /// Translates a composite format string into the current UI language and formats it with the current culture.
    /// </summary>
    /// <param name="format">The Ukrainian format string with <c>{0}</c>-style placeholders.</param>
    /// <param name="args">The values.</param>
    /// <returns>The formatted translation.</returns>
    public static string T(string format, params object?[] args)
        => string.Format(CultureInfo.CurrentCulture, T(format), args);

    /// <summary>
    /// Translates a text whose English depends on where it is shown: the same Ukrainian word can need different words
    /// («Довідка» is Help in the menu and Certificate as a document kind; «Зміна пароля» is an audit event and a button).
    /// Looks up the key <c>"&lt;context&gt;|&lt;text&gt;"</c> first, then the text alone.
    /// </summary>
    /// <param name="context">The usage context (e.g. <c>audit</c>, <c>document-kind</c>, <c>help</c>).</param>
    /// <param name="text">The Ukrainian text.</param>
    /// <returns>The translation, or <paramref name="text"/> when the language is Ukrainian or no translation exists.</returns>
    public static string In(string context, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var language = Languages.Current;
        if (language != Languages.Ukrainian
            && Catalogs.TryGetValue(language, out var catalog)
            && catalog.TryGetValue(context + "|" + text, out var translation))
        {
            return translation;
        }

        return Translate(text, language);
    }

    /// <summary>
    /// Translates a text into the given language.
    /// </summary>
    /// <param name="text">The Ukrainian text.</param>
    /// <param name="language">The language code (<see cref="Languages"/>).</param>
    /// <returns>The translation, or <paramref name="text"/> when none exists.</returns>
    public static string Translate(string text, string language)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (language != Languages.Ukrainian
            && Catalogs.TryGetValue(language, out var catalog)
            && catalog.TryGetValue(text, out var translation))
        {
            return translation;
        }

        return text;
    }

    /// <summary>
    /// Determines whether a translation of the text into the language exists.
    /// </summary>
    /// <param name="text">The Ukrainian text.</param>
    /// <param name="language">The language code.</param>
    /// <returns><see langword="true"/> when translated.</returns>
    public static bool HasTranslation(string text, string language)
        => Catalogs.TryGetValue(language, out var catalog) && catalog.ContainsKey(text);

    /// <summary>
    /// Loads the embedded translation files of an assembly (once per assembly).
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public static void Register(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (assembly.IsDynamic
            || assembly.GetName().Name is not { } name
            || !name.StartsWith("KOD.", StringComparison.Ordinal)
            || !LoadedAssemblies.TryAdd(name, true))
        {
            return;
        }

        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            // "kod-i18n:en\documents.json" (the separator follows the build machine).
            var path = resource[ResourcePrefix.Length..].Split('\\', '/');
            if (path.Length < 2)
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resource)!;
            var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
            var catalog = Catalogs.GetOrAdd(path[0], static _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal));
            foreach (var (key, value) in entries)
            {
                catalog.TryAdd(key, value);
            }
        }
    }
}
