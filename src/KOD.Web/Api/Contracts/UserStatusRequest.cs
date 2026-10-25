namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>PUT /api/v1/users/{id}/status</c>.
/// </summary>
/// <param name="IsActive"><see langword="true"/> to activate, <see langword="false"/> to block the account.</param>
public sealed record UserStatusRequest(bool IsActive);
