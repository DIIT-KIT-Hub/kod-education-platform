using ODC.Application.Common.Models;
using ODC.Domain.Tickets;

namespace ODC.Application.Features.Tickets;

/// <summary>
/// A request with its whole history and what the current user may do with it.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Number">The number shown to people.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Category">What it is about.</param>
/// <param name="Priority">How urgent it is.</param>
/// <param name="Status">The state.</param>
/// <param name="Author">The author.</param>
/// <param name="AuthorPosition">The main position of the author (for the handlers).</param>
/// <param name="AuthorEmail">The e-mail of the author (for the handlers).</param>
/// <param name="Assignee">The handler who works on it.</param>
/// <param name="CreatedAt">The moment it was submitted.</param>
/// <param name="LastActivityAt">The moment of the last message or change.</param>
/// <param name="Messages">The history, oldest first (the first message is the description).</param>
/// <param name="IsMine">Whether the current user submitted it.</param>
/// <param name="CanManage">Whether the current user handles requests (state, handler, category).</param>
/// <param name="CanReply">Whether the current user may write in it now.</param>
/// <param name="CanClose">Whether the current user may close it.</param>
public sealed record TicketDetails(
    Guid Id,
    int Number,
    string Subject,
    TicketCategory Category,
    TicketPriority Priority,
    TicketStatus Status,
    UserRef Author,
    string? AuthorPosition,
    string? AuthorEmail,
    UserRef? Assignee,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    IReadOnlyList<TicketMessageDto> Messages,
    bool IsMine,
    bool CanManage,
    bool CanReply,
    bool CanClose);
