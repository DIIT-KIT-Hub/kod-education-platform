using System.ComponentModel.DataAnnotations;
using ODC.Domain.Chats;

namespace ODC.Web.Api.Contracts;

/// <summary>
/// Body of <c>PUT /api/v1/chats/{id}/title</c>.
/// </summary>
/// <param name="Title">The new title, or empty to show the members' names.</param>
public sealed record ChatTitleRequest([MaxLength(Chat.TitleMaxLength)] string? Title);
