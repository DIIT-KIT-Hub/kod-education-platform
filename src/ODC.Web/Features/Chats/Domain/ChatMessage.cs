using System.Diagnostics.CodeAnalysis;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Features.Chats.Domain;

/// <summary>
/// A message of a chat: formatted text (sanitized HTML) with attachments, or a system note. The number
/// (<see cref="Sequence"/>) orders the chat; the client identifier (<see cref="ClientId"/>) makes a resent message
/// (lost answer, reconnect) land only once.
/// </summary>
public sealed class ChatMessage : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Body"/> (HTML).
    /// </summary>
    public const int BodyMaxLength = 20_000;

    /// <summary>
    /// Maximum length of <see cref="Text"/> (the visible text).
    /// </summary>
    public const int TextMaxLength = 5_000;

    /// <summary>
    /// The largest number of attachments of one message.
    /// </summary>
    public const int MaxAttachments = 10;

    /// <summary>
    /// The attachments.
    /// </summary>
    private readonly List<ChatAttachment> _attachments = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class: a message written by a member.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="sequence">The number in the chat.</param>
    /// <param name="authorId">The author.</param>
    /// <param name="clientId">The identifier the author's browser gave the message.</param>
    /// <param name="body">The sanitized HTML.</param>
    /// <param name="text">The visible text of <paramref name="body"/>.</param>
    /// <param name="attachments">The attachments.</param>
    public ChatMessage(Guid chatId, long sequence, Guid authorId, Guid clientId, string body, string text, IReadOnlyCollection<ChatAttachment> attachments)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        if (attachments.Count > MaxAttachments)
        {
            throw new DomainException(Texts.T("До повідомлення можна додати не більше {0} файлів.", MaxAttachments));
        }

        ChatId = chatId;
        Sequence = sequence;
        AuthorId = authorId;
        ClientId = clientId;
        Kind = ChatMessageKind.Text;
        SetContent(body, text, attachments.Count > 0);
        foreach (var attachment in attachments)
        {
            attachment.AttachTo(this, _attachments.Count);
            _attachments.Add(attachment);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class: a system note.
    /// </summary>
    /// <param name="chatId">The chat.</param>
    /// <param name="sequence">The number in the chat.</param>
    /// <param name="actorId">The person whose action the note tells about.</param>
    /// <param name="chatEvent">What happened.</param>
    /// <param name="details">The new title or the names of the people concerned.</param>
    public ChatMessage(Guid chatId, long sequence, Guid actorId, ChatEvent chatEvent, string? details)
    {
        ChatId = chatId;
        Sequence = sequence;
        AuthorId = actorId;
        Kind = ChatMessageKind.System;
        Event = chatEvent;
        Body = string.Empty;
        Text = details is { Length: > TextMaxLength } ? details[..TextMaxLength] : details ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessage"/> class for EF Core materialization.
    /// </summary>
    private ChatMessage()
    {
        Body = string.Empty;
        Text = string.Empty;
    }

    /// <summary>
    /// Gets the chat.
    /// </summary>
    public Guid ChatId { get; private set; }

    /// <summary>
    /// Gets the number in the chat (1, 2, 3…, without gaps).
    /// </summary>
    public long Sequence { get; private set; }

    /// <summary>
    /// Gets the author (for a system note, the person who acted).
    /// </summary>
    public Guid AuthorId { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public User? Author { get; private set; }

    /// <summary>
    /// Gets the author of the original message when this one was forwarded from another chat (the first author: a
    /// forwarded message forwarded again keeps them).
    /// </summary>
    public Guid? ForwardedFromId { get; private set; }

    /// <summary>
    /// Gets the author of the original message of a forwarded one.
    /// </summary>
    public User? ForwardedFrom { get; private set; }

    /// <summary>
    /// Gets the identifier the author's browser gave the message (unique per author and chat).
    /// </summary>
    public Guid? ClientId { get; private set; }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    public ChatMessageKind Kind { get; private set; }

    /// <summary>
    /// Gets what a system note tells.
    /// </summary>
    public ChatEvent? Event { get; private set; }

    /// <summary>
    /// Gets the sanitized HTML (empty for a system note and once deleted).
    /// </summary>
    public string Body { get; private set; }

    /// <summary>
    /// Gets the visible text (previews, notifications; the details of a system note).
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// Gets the moment of the last edit.
    /// </summary>
    public DateTimeOffset? EditedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the message was deleted (a placeholder stays in the conversation).
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the attachments.
    /// </summary>
    public IReadOnlyCollection<ChatAttachment> Attachments => _attachments;

    /// <summary>
    /// Replaces the text (the attachments stay).
    /// </summary>
    /// <param name="body">The sanitized HTML.</param>
    /// <param name="text">The visible text of <paramref name="body"/>.</param>
    /// <param name="moment">The current moment.</param>
    public void Edit(string body, string text, DateTimeOffset moment)
    {
        if (Kind != ChatMessageKind.Text || IsDeleted)
        {
            throw new DomainException(Texts.T("Це повідомлення не можна змінити."));
        }

        if (ForwardedFromId is not null)
        {
            throw new DomainException(Texts.T("Переслане повідомлення не можна змінити."));
        }

        SetContent(body, text, _attachments.Count > 0);
        EditedAt = moment;
    }

    /// <summary>
    /// Marks the message as forwarded: it shows the author of the original message.
    /// </summary>
    /// <param name="originalAuthorId">The author of the original message.</param>
    public void MarkForwarded(Guid originalAuthorId)
    {
        if (Kind != ChatMessageKind.Text)
        {
            throw new DomainException(Texts.T("Це повідомлення не можна переслати."));
        }

        ForwardedFromId = originalAuthorId;
    }

    /// <summary>
    /// Deletes the content and the attachments; the caller removes their files.
    /// </summary>
    /// <returns>The removed attachments.</returns>
    public IReadOnlyList<ChatAttachment> Delete()
    {
        if (Kind != ChatMessageKind.Text || IsDeleted)
        {
            throw new DomainException(Texts.T("Це повідомлення не можна видалити."));
        }

        var removed = _attachments.ToList();
        _attachments.Clear();
        IsDeleted = true;
        Body = string.Empty;
        Text = string.Empty;
        return removed;
    }

    /// <summary>
    /// Checks and sets the text.
    /// </summary>
    /// <param name="body">The sanitized HTML.</param>
    /// <param name="text">The visible text.</param>
    /// <param name="hasAttachments">Whether the message has attachments (then the text may be empty).</param>
    [MemberNotNull(nameof(Body), nameof(Text))]
    private void SetContent(string body, string text, bool hasAttachments)
    {
        body ??= string.Empty;
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0 && !hasAttachments)
        {
            throw new DomainException(Texts.T("Напишіть повідомлення або додайте файл."));
        }

        if (body.Length > BodyMaxLength || text.Length > TextMaxLength)
        {
            throw new DomainException(Texts.T("Повідомлення задовге: не більше {0} символів.", TextMaxLength));
        }

        Body = text.Length == 0 ? string.Empty : body;
        Text = text;
    }
}
