namespace KOD.Web.Api.Contracts;

/// <summary>
/// Answer of <c>GET /api/v1/notifications/unread-count</c>.
/// </summary>
/// <param name="Count">The number of unread notifications.</param>
public sealed record UnreadCountResponse(int Count);
