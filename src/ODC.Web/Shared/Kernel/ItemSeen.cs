namespace ODC.Web.Shared.Kernel;

/// <summary>
/// A user has seen a document, a request or a chat (read to the end), or it is gone for them: whatever told them about
/// it is done.
/// </summary>
/// <param name="UserId">The user.</param>
/// <param name="ItemId">The document, request or chat.</param>
public sealed record ItemSeen(Guid UserId, Guid ItemId) : IDomainEvent;
