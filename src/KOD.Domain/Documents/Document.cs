using KOD.Domain.Common;
using KOD.Domain.Organization;
using KOD.Domain.Users;

namespace KOD.Domain.Documents;

/// <summary>
/// An electronic document with its signing route and collected signatures (aggregate root).
/// </summary>
public sealed class Document : AuditableEntity
{
    /// <summary>
    /// Maximum length of <see cref="Title"/>.
    /// </summary>
    public const int TitleMaxLength = 300;

    /// <summary>
    /// Maximum length of <see cref="Description"/>.
    /// </summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>
    /// Maximum number of signers on a route.
    /// </summary>
    public const int MaxSigners = 50;

    /// <summary>
    /// The signing route.
    /// </summary>
    private readonly List<DocumentSigner> _signers = [];

    /// <summary>
    /// The collected signatures.
    /// </summary>
    private readonly List<DocumentSignature> _signatures = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Document"/> class.
    /// </summary>
    /// <param name="registrationNumber">The unique registration number.</param>
    /// <param name="title">The title.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="kind">The document kind.</param>
    /// <param name="authorId">The author's user identifier.</param>
    /// <param name="organizationUnitId">The unit the document belongs to.</param>
    /// <param name="signingMode">The signing order.</param>
    /// <param name="file">The document file.</param>
    public Document(
        string registrationNumber,
        string title,
        string? description,
        DocumentKind kind,
        Guid authorId,
        Guid organizationUnitId,
        SigningMode signingMode,
        StoredFile file)
    {
        RegistrationNumber = Guard.Text(registrationNumber, 32, "Реєстраційний номер");
        Title = Guard.Text(title, TitleMaxLength, "Назва");
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Опис");
        Kind = kind;
        AuthorId = authorId;
        OrganizationUnitId = organizationUnitId;
        SigningMode = signingMode;
        File = file ?? throw new ArgumentNullException(nameof(file));
        Status = DocumentStatus.Draft;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Document"/> class for EF Core materialization.
    /// </summary>
    private Document()
    {
        RegistrationNumber = string.Empty;
        Title = string.Empty;
        File = null!;
    }

    /// <summary>
    /// Gets the unique registration number (e.g. <c>2026/000123</c>).
    /// </summary>
    public string RegistrationNumber { get; private set; }

    /// <summary>
    /// Gets the title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the document kind.
    /// </summary>
    public DocumentKind Kind { get; private set; }

    /// <summary>
    /// Gets the life-cycle state.
    /// </summary>
    public DocumentStatus Status { get; private set; }

    /// <summary>
    /// Gets the signing order.
    /// </summary>
    public SigningMode SigningMode { get; private set; }

    /// <summary>
    /// Gets the author's user identifier.
    /// </summary>
    public Guid AuthorId { get; private set; }

    /// <summary>
    /// Gets the author.
    /// </summary>
    public User? Author { get; private set; }

    /// <summary>
    /// Gets the identifier of the unit the document belongs to.
    /// </summary>
    public Guid OrganizationUnitId { get; private set; }

    /// <summary>
    /// Gets the unit the document belongs to.
    /// </summary>
    public OrganizationUnit? OrganizationUnit { get; private set; }

    /// <summary>
    /// Gets the document file.
    /// </summary>
    public StoredFile File { get; private set; }

    /// <summary>
    /// Gets the moment the document was sent for signing.
    /// </summary>
    public DateTimeOffset? SubmittedAt { get; private set; }

    /// <summary>
    /// Gets the moment the route was completed (signed or rejected).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the signing route ordered by <see cref="DocumentSigner.Order"/>.
    /// </summary>
    public IReadOnlyCollection<DocumentSigner> Signers => _signers;

    /// <summary>
    /// Gets the collected signatures.
    /// </summary>
    public IReadOnlyCollection<DocumentSignature> Signatures => _signatures;

    /// <summary>
    /// Gets a value indicating whether the author may change the document: a draft, or a rejected document that is being
    /// corrected before it is sent again.
    /// </summary>
    public bool IsEditable => Status is DocumentStatus.Draft or DocumentStatus.Rejected;

    /// <summary>
    /// Updates the descriptive data of a draft or a rejected document. These details are not part of the signed file,
    /// so existing signatures stay valid.
    /// </summary>
    /// <param name="title">The new title.</param>
    /// <param name="description">The new description.</param>
    /// <param name="kind">The new kind.</param>
    /// <param name="organizationUnitId">The unit the document belongs to.</param>
    public void UpdateDetails(string title, string? description, DocumentKind kind, Guid organizationUnitId)
    {
        EnsureEditable();
        Title = Guard.Text(title, TitleMaxLength, "Назва");
        Description = Guard.OptionalText(description, DescriptionMaxLength, "Опис");
        Kind = kind;
        OrganizationUnitId = organizationUnitId;
    }

    /// <summary>
    /// Replaces the file of a draft or a rejected document. Every signature was made on the previous file, so all of them
    /// are removed; the caller deletes their stored files.
    /// </summary>
    /// <param name="file">The new file.</param>
    /// <returns>The removed signatures.</returns>
    public IReadOnlyList<DocumentSignature> ReplaceFile(StoredFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        EnsureEditable();

        File = file;
        var removed = _signatures.ToList();
        _signatures.Clear();
        return removed;
    }

    /// <summary>
    /// Replaces the signing route of a draft or a rejected document. Signatures of users who are no longer on the route
    /// are removed (the caller deletes their stored files); signatures of the remaining signers are kept.
    /// </summary>
    /// <param name="signerIds">The signers in signing order.</param>
    /// <param name="mode">The signing order mode.</param>
    /// <returns>The removed signatures.</returns>
    public IReadOnlyList<DocumentSignature> SetRoute(IReadOnlyList<Guid> signerIds, SigningMode mode)
    {
        ArgumentNullException.ThrowIfNull(signerIds);
        EnsureEditable();

        var distinct = signerIds.Distinct().ToList();
        if (distinct.Count > MaxSigners)
        {
            throw new DomainException($"Маршрут не може містити більше {MaxSigners} підписантів.");
        }

        var unchanged = mode == SigningMode && _signers.OrderBy(static s => s.Order).Select(static s => s.UserId).SequenceEqual(distinct);
        if (!unchanged)
        {
            _signers.Clear();
            _signers.AddRange(distinct.Select((userId, index) => new DocumentSigner(Id, userId, index + 1)));
            SigningMode = mode;
        }

        var removed = _signatures.Where(s => !distinct.Contains(s.UserId)).ToList();
        _signatures.RemoveAll(removed.Contains);
        return removed;
    }

    /// <summary>
    /// Sends the draft (or a corrected rejected document) for signing. Signers who already signed the current file keep
    /// their signatures and are not asked again; everyone else signs (again). When nobody is left, the document is signed.
    /// </summary>
    /// <param name="moment">The current moment.</param>
    public void Submit(DateTimeOffset moment)
    {
        if (!IsEditable)
        {
            throw new DomainException("Надіслати на підпис можна лише чернетку або відхилений документ.");
        }

        if (_signers.Count == 0)
        {
            throw new DomainException("Додайте щонайменше одного підписанта.");
        }

        foreach (var signer in _signers)
        {
            var signature = _signatures.Where(s => s.UserId == signer.UserId).MaxBy(static s => s.SignedAt);
            if (signature is null)
            {
                signer.Reset();
            }
            else
            {
                signer.MarkSigned(signature.SignedAt);
            }
        }

        Status = DocumentStatus.InSigning;
        SubmittedAt = moment;
        CompletedAt = null;
        UpdatedAt = moment;

        if (_signers.TrueForAll(static s => s.Status == SignerStatus.Signed))
        {
            Status = DocumentStatus.Signed;
            CompletedAt = moment;
        }
    }

    /// <summary>
    /// Returns a document from signing back to the draft state (only before anyone signed it).
    /// </summary>
    public void Withdraw()
    {
        EnsureStatus(DocumentStatus.InSigning, "Відкликати можна лише документ на підписанні.");
        if (_signatures.Count > 0)
        {
            throw new DomainException("Документ уже має підписи, його не можна відкликати.");
        }

        Status = DocumentStatus.Draft;
        SubmittedAt = null;
    }

    /// <summary>
    /// Determines whether the given user is the signer whose turn it is now.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns><see langword="true"/> when the user may sign or reject the document now.</returns>
    public bool IsAwaitingSignatureFrom(Guid userId)
    {
        if (Status != DocumentStatus.InSigning)
        {
            return false;
        }

        var step = _signers.Find(s => s.UserId == userId && s.Status == SignerStatus.Pending);
        if (step is null)
        {
            return false;
        }

        return SigningMode == SigningMode.Parallel
            || _signers.Where(static s => s.Status == SignerStatus.Pending).Min(static s => s.Order) == step.Order;
    }

    /// <summary>
    /// Attaches a signature created by the current signer and advances the route.
    /// </summary>
    /// <param name="userId">The signer's user identifier.</param>
    /// <param name="format">The signature format.</param>
    /// <param name="packaging">The signature packaging.</param>
    /// <param name="level">The signature level.</param>
    /// <param name="keyMedia">The type of the key storage.</param>
    /// <param name="file">The stored signature file.</param>
    /// <param name="certificate">The signer's certificate data.</param>
    /// <param name="signedAt">The signing time.</param>
    /// <param name="moment">The current moment.</param>
    /// <returns>The created signature.</returns>
    public DocumentSignature AddSignature(
        Guid userId,
        SignatureFormat format,
        SignaturePackaging packaging,
        string? level,
        KeyMediaType keyMedia,
        StoredFile file,
        SignerCertificate certificate,
        DateTimeOffset signedAt,
        DateTimeOffset moment)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(certificate);

        if (!IsAwaitingSignatureFrom(userId))
        {
            throw new DomainException("Документ зараз не очікує вашого підпису.");
        }

        if (format == SignatureFormat.PAdES && !File.IsPdf)
        {
            throw new DomainException("Формат PAdES доступний лише для PDF-документів.");
        }

        var signature = new DocumentSignature(Id, userId, format, packaging, level, keyMedia, file, certificate, signedAt, moment);
        _signatures.Add(signature);

        // Touching the root makes concurrent signers collide on the optimistic concurrency token.
        UpdatedAt = moment;
        _signers.First(s => s.UserId == userId && s.Status == SignerStatus.Pending).MarkSigned(moment);

        if (_signers.TrueForAll(static s => s.Status == SignerStatus.Signed))
        {
            Status = DocumentStatus.Signed;
            CompletedAt = moment;
        }

        return signature;
    }

    /// <summary>
    /// Declines to sign the document.
    /// </summary>
    /// <param name="userId">The signer's user identifier.</param>
    /// <param name="reason">The reason of the rejection.</param>
    /// <param name="moment">The current moment.</param>
    public void Reject(Guid userId, string reason, DateTimeOffset moment)
    {
        if (!IsAwaitingSignatureFrom(userId))
        {
            throw new DomainException("Документ зараз не очікує вашого рішення.");
        }

        _signers.First(s => s.UserId == userId && s.Status == SignerStatus.Pending).MarkRejected(reason, moment);
        UpdatedAt = moment;
        Status = DocumentStatus.Rejected;
        CompletedAt = moment;
    }

    /// <summary>
    /// Moves a completed document to the archive.
    /// </summary>
    public void Archive()
    {
        if (Status is not (DocumentStatus.Signed or DocumentStatus.Rejected))
        {
            throw new DomainException("Архівувати можна лише підписаний або відхилений документ.");
        }

        Status = DocumentStatus.Archived;
    }

    /// <summary>
    /// Ensures that a draft may be deleted.
    /// </summary>
    public void EnsureCanBeDeleted()
    {
        if (_signatures.Count > 0)
        {
            throw new DomainException("Документ із підписами не можна видалити — його можна лише архівувати.");
        }
    }

    /// <summary>
    /// Throws when the document is not in the expected state.
    /// </summary>
    /// <param name="expected">The expected state.</param>
    /// <param name="message">The user-facing error message.</param>
    private void EnsureStatus(DocumentStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainException(message);
        }
    }

    /// <summary>
    /// Throws when the document may not be changed.
    /// </summary>
    private void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new DomainException("Змінювати можна лише чернетку або відхилений документ.");
        }
    }
}
