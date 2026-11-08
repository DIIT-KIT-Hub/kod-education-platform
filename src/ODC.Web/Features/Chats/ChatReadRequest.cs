using System.ComponentModel.DataAnnotations;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Body of <c>PUT /api/v1/chats/{id}/read</c>.
/// </summary>
/// <param name="Sequence">The number of the last read message.</param>
public sealed record ChatReadRequest([Range(0, long.MaxValue)] long Sequence);
