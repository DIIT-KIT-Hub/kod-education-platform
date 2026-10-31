namespace KOD.Web.Api.Contracts;

/// <summary>
/// A change of the interface language.
/// </summary>
/// <param name="Language">The language code (<c>uk</c>, <c>en</c>).</param>
public sealed record LanguageRequest(string Language);
