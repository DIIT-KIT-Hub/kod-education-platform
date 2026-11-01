using KOD.Application.Common.Models;
using KOD.Domain.Notifications;

namespace KOD.Application.Features.Notifications;

/// <summary>
/// A notification for display.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Kind">What happened (the client translates it into the title).</param>
/// <param name="Actor">Who did it.</param>
/// <param name="EntityId">The document, article, chat or request concerned (the client refreshes its page).</param>
/// <param name="Subject">The title of what is concerned.</param>
/// <param name="Preview">The start of the text.</param>
/// <param name="Code">A machine value shown translated (the new state of a request).</param>
/// <param name="Link">The address of the page to open.</param>
/// <param name="Count">How many events it collects (messages of a chat).</param>
/// <param name="CreatedAt">The moment of the last event.</param>
/// <param name="ReadAt">The moment it was read.</param>
public sealed record NotificationDto(
    Guid Id,
    NotificationKind Kind,
    UserRef? Actor,
    Guid EntityId,
    string Subject,
    string? Preview,
    string? Code,
    string Link,
    int Count,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);
