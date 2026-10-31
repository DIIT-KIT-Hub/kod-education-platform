namespace KOD.Web.Api.Contracts;

/// <summary>
/// Recovery codes, shown once.
/// </summary>
/// <param name="Codes">The codes.</param>
public sealed record RecoveryCodesResponse(IReadOnlyList<string> Codes);
