using KOD.Application.Common.Models;
using KOD.Domain.Tickets;

namespace KOD.Application.Features.Tickets;

/// <summary>
/// A request in a list.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Number">The number shown to people.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Category">What it is about.</param>
/// <param name="Priority">How urgent it is.</param>
/// <param name="Status">The state.</param>
/// <param name="Author">The author.</param>
/// <param name="Assignee">The handler who works on it.</param>
/// <param name="CreatedAt">The moment it was submitted.</param>
/// <param name="LastActivityAt">The moment of the last message or change.</param>
/// <param name="CommentCount">The number of messages (the description included).</param>
/// <param name="AwaitingAnswer">Whether the last message is the author's (the handlers owe an answer).</param>
public sealed record TicketListItem(
    Guid Id,
    int Number,
    string Subject,
    TicketCategory Category,
    TicketPriority Priority,
    TicketStatus Status,
    UserRef Author,
    UserRef? Assignee,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    int CommentCount,
    bool AwaitingAnswer);
