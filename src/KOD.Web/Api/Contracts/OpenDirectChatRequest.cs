namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>POST /api/v1/chats/direct</c>.
/// </summary>
/// <param name="UserId">The person to chat with.</param>
public sealed record OpenDirectChatRequest(Guid UserId);
