namespace ODC.Web.Features.Auth;

/// <summary>
/// Recovery codes, shown once.
/// </summary>
/// <param name="Codes">The codes.</param>
public sealed record RecoveryCodesResponse(IReadOnlyList<string> Codes);
