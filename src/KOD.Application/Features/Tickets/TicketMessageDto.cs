using KOD.Application.Common.Models;
using KOD.Domain.Tickets;

namespace KOD.Application.Features.Tickets;

/// <summary>
/// An entry in the history of a request.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Author">The writer (for a note, who made the change).</param>
/// <param name="FromAuthor">Whether the author of the request wrote it (otherwise a handler).</param>
/// <param name="Body">The plain text (the name of the new handler for an assignment note).</param>
/// <param name="Status">The new state of a state note.</param>
/// <param name="CreatedAt">The moment it was written.</param>
/// <param name="Attachments">The files.</param>
public sealed record TicketMessageDto(
    Guid Id,
    TicketMessageKind Kind,
    UserRef Author,
    bool FromAuthor,
    string Body,
    TicketStatus? Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TicketAttachmentDto> Attachments);
