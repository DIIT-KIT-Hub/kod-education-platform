using System.ComponentModel.DataAnnotations;
using ODC.Web.Features.Chats.Domain;

namespace ODC.Web.Features.Chats;

/// <summary>
/// Body of <c>PUT /api/v1/chats/{id}/title</c>.
/// </summary>
/// <param name="Title">The new title, or empty to show the members' names.</param>
public sealed record ChatTitleRequest([MaxLength(Chat.TitleMaxLength)] string? Title);
