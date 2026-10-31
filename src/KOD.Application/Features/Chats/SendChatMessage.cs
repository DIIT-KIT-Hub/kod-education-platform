using KOD.Application.Common.Files;

namespace KOD.Application.Features.Chats;

/// <summary>
/// A message to send.
/// </summary>
/// <param name="ClientId">The identifier the browser gave the message; a resent message with it lands only once.</param>
/// <param name="Body">The HTML of the editor (sanitized by the service).</param>
/// <param name="Files">The attached files.</param>
public sealed record SendChatMessage(Guid ClientId, string? Body, IReadOnlyList<FileUpload> Files);
