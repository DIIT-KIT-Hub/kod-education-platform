using KOD.Domain.Common;
using KOD.Domain.Users;

namespace KOD.Domain.Documents;

/// <summary>
/// A step of a document's signing route: a user who has to sign the document.
/// </summary>
public sealed class DocumentSigner : Entity
{
    /// <summary>
    /// Maximum length of <see cref="Comment"/>.
    /// </summary>
    public const int CommentMaxLength = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSigner"/> class.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="userId">The signer's user identifier.</param>
    /// <param name="order">The 1-based position in the route.</param>
    internal DocumentSigner(Guid documentId, Guid userId, int order)
    {
        DocumentId = documentId;
        UserId = userId;
        Order = order;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSigner"/> class for EF Core materialization.
    /// </summary>
    private DocumentSigner()
    {
    }

    /// <summary>
    /// Gets the document identifier.
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Gets the signer's user identifier.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the signer.
    /// </summary>
    public User? User { get; private set; }

    /// <summary>
    /// Gets the 1-based position in the route.
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// Gets the decision state.
    /// </summary>
    public SignerStatus Status { get; private set; }

    /// <summary>
    /// Gets the moment of the decision.
    /// </summary>
    public DateTimeOffset? DecidedAt { get; private set; }

    /// <summary>
    /// Gets the signer's comment (mandatory when rejecting).
    /// </summary>
    public string? Comment { get; private set; }

    /// <summary>
    /// Marks the step as signed.
    /// </summary>
    /// <param name="moment">The decision moment.</param>
    internal void MarkSigned(DateTimeOffset moment)
    {
        Status = SignerStatus.Signed;
        DecidedAt = moment;
    }

    /// <summary>
    /// Marks the step as rejected.
    /// </summary>
    /// <param name="reason">The reason of the rejection.</param>
    /// <param name="moment">The decision moment.</param>
    internal void MarkRejected(string reason, DateTimeOffset moment)
    {
        // Validate before changing any state.
        Comment = Guard.Text(reason, CommentMaxLength, "Причина відмови");
        Status = SignerStatus.Rejected;
        DecidedAt = moment;
    }

    /// <summary>
    /// Resets the step to the pending state (used when a rejected document is resubmitted).
    /// </summary>
    internal void Reset()
    {
        Status = SignerStatus.Pending;
        DecidedAt = null;
        Comment = null;
    }
}
