using ODC.Application.Common.Files;
using ODC.Domain.Tickets;

namespace ODC.Application.Features.Tickets;

/// <summary>
/// A new request.
/// </summary>
/// <param name="Category">What it is about.</param>
/// <param name="Priority">How urgent it is.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Description">The description.</param>
/// <param name="Files">The attached files and screenshots.</param>
public sealed record SubmitTicket(TicketCategory Category, TicketPriority Priority, string Subject, string Description, IReadOnlyList<FileUpload> Files);
