namespace ODC.Web.Features.Notifications.Domain;

/// <summary>
/// What a notification tells about. The client builds the translated title from it.
/// </summary>
public enum NotificationKind
{
    /// <summary>Someone answered the user's comment on a news article.</summary>
    NewsReply = 0,

    /// <summary>A document waits for the user's signature.</summary>
    DocumentToSign = 1,

    /// <summary>A document of the user got every signature of its route.</summary>
    DocumentSigned = 2,

    /// <summary>A signer declined to sign a document of the user.</summary>
    DocumentRejected = 3,

    /// <summary>New messages in a chat (one notification per chat until it is read).</summary>
    ChatMessage = 4,

    /// <summary>A new request was submitted (for the people who handle requests).</summary>
    TicketCreated = 5,

    /// <summary>A new answer in a request.</summary>
    TicketReply = 6,

    /// <summary>The state of a request changed.</summary>
    TicketStatusChanged = 7,

    /// <summary>A request was assigned to the user.</summary>
    TicketAssigned = 8,
}
