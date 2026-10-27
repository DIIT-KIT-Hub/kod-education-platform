using KOD.Application.Abstractions;
using KOD.Application.Common.Access;
using KOD.Application.Common.Errors;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Common.Results;
using KOD.Application.Common.Text;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Documents;
using KOD.Domain.Localization;
using Microsoft.EntityFrameworkCore;

namespace KOD.Application.Features.Documents;

/// <summary>
/// Default <see cref="IDocumentService"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="accessScope">The data-level access rules.</param>
/// <param name="fileStorage">The private file storage.</param>
/// <param name="numbers">The registration number generator.</param>
/// <param name="signatureInspector">The signature container inspector.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="timeProvider">The clock.</param>
internal sealed class DocumentService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IAccessScope accessScope,
    IFileStorage fileStorage,
    IDocumentNumberGenerator numbers,
    ISignatureInspector signatureInspector,
    IAuditLogger audit,
    TimeProvider timeProvider) : IDocumentService
{
    /// <summary>
    /// Containers up to this size are parsed on the server to extract the signer certificate.
    /// </summary>
    private const long InspectableSignatureSize = 10L * 1024 * 1024;

    /// <summary>
    /// How many times a signature is attached when concurrent signers of a parallel route keep saving first.
    /// </summary>
    private const int MaxSaveAttempts = 3;

    /// <summary>
    /// The error code of "another signer holds the signing turn" (the browser waits and asks again).
    /// </summary>
    private const string SigningTurnErrorCode = "signing_turn";

    /// <summary>
    /// Allowed clock skew between the signer's browser and the server.
    /// </summary>
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public async Task<Result<PagedResult<DocumentListItem>>> ListAsync(DocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        if (!currentUser.Has(Permissions.Documents.View))
        {
            return Error.Forbidden();
        }

        var documents = db.Documents.AsNoTracking();
        documents = query.Scope switch
        {
            DocumentScope.AwaitingMe => documents.Where(d =>
                d.Status == DocumentStatus.InSigning
                && d.Signers.Any(s => s.UserId == me && s.Status == SignerStatus.Pending
                    && (d.SigningMode == SigningMode.Parallel
                        || !d.Signers.Any(o => o.Status == SignerStatus.Pending && o.Order < s.Order)))),
            DocumentScope.Authored => documents.Where(d => d.AuthorId == me),
            DocumentScope.Involved => documents.Where(d => d.Signers.Any(s => s.UserId == me)),
            DocumentScope.SignedByMe => documents.Where(d => d.Signers.Any(s => s.UserId == me && s.Status == SignerStatus.Signed)),
            _ => await accessScope.VisibleDocumentsAsync(documents, cancellationToken),
        };

        if (query.Status is { } status)
        {
            documents = documents.Where(d => d.Status == status);
        }

        if (query.Kind is { } kind)
        {
            documents = documents.Where(d => d.Kind == kind);
        }

        if (query.Search is { } search)
        {
            var term = search;
            documents = documents.Where(d => d.Title.Contains(term) || d.RegistrationNumber.Contains(term));
        }

        documents = query.SortBy switch
        {
            "number" => documents.OrderByDirection(static d => d.RegistrationNumber, query.Descending),
            "title" => documents.OrderByDirection(static d => d.Title, query.Descending),
            "status" => documents.OrderByDirection(static d => d.Status, query.Descending),
            "created" => documents.OrderByDirection(static d => d.CreatedAt, query.Descending),
            _ => documents.OrderByDescending(static d => d.CreatedAt),
        };

        return await documents
            .Select(d => new DocumentListItem(
                d.Id,
                d.RegistrationNumber,
                d.Title,
                d.Kind,
                d.Status,
                d.Author!.LastName + " " + d.Author.FirstName,
                d.OrganizationUnit!.Name,
                d.CreatedAt,
                d.Signers.Count(static s => s.Status == SignerStatus.Signed),
                d.Signers.Count,
                d.Status == DocumentStatus.InSigning
                    && d.Signers.Any(s => s.UserId == me && s.Status == SignerStatus.Pending
                        && (d.SigningMode == SigningMode.Parallel
                            || !d.Signers.Any(o => o.Status == SignerStatus.Pending && o.Order < s.Order))),
                d.Author.PhotoId))
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDetails>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        if (!await IsVisibleAsync(id, cancellationToken))
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        var document = await db.Documents
            .AsNoTracking()
            .Include(static d => d.Author)
            .Include(static d => d.OrganizationUnit)
            .Include(static d => d.Signers).ThenInclude(static s => s.User)
            .Include(static d => d.Signatures).ThenInclude(static s => s.User)
            .FirstAsync(d => d.Id == id, cancellationToken);

        var signerIds = document.Signers.Select(static s => s.UserId).ToList();
        var positions = await db.StaffPositions
            .AsNoTracking()
            .Where(p => signerIds.Contains(p.UserId) && p.IsPrimary)
            .Select(static p => new { p.UserId, p.Title, Role = p.Role!.Name, Unit = p.OrganizationUnit!.Name })
            .ToDictionaryAsync(static p => p.UserId, static p => StoredText.PositionInUnit(p.Title, p.Role, p.Unit)!, cancellationToken);

        var isAuthor = document.AuthorId == me;
        var canManage = currentUser.Has(Permissions.Documents.Manage);
        var awaitingMe = document.IsAwaitingSignatureFrom(me) && currentUser.Has(Permissions.Documents.Sign);
        var hasSignatures = document.Signatures.Count > 0;
        var actions = new DocumentActions(
            CanEdit: isAuthor && document.IsEditable && currentUser.Has(Permissions.Documents.Create),
            CanSubmit: isAuthor && document.IsEditable && document.Signers.Count > 0,
            CanWithdraw: isAuthor && document.Status == DocumentStatus.InSigning && !hasSignatures,
            CanSign: awaitingMe,
            CanReject: awaitingMe,
            CanArchive: (isAuthor || canManage) && document.Status is DocumentStatus.Signed or DocumentStatus.Rejected,
            CanDelete: (isAuthor || canManage) && !hasSignatures && document.Status != DocumentStatus.Archived);

        return new DocumentDetails(
            document.Id,
            document.RegistrationNumber,
            document.Title,
            document.Description,
            document.Kind,
            document.Status,
            document.SigningMode,
            new UserRef(document.AuthorId, document.Author!.FullName, document.Author.PhotoId),
            new UnitRef(document.OrganizationUnitId, document.OrganizationUnit!.Name),
            new DocumentFileInfo(document.File.FileName, document.File.ContentType, document.File.Size, document.File.Sha256, document.File.IsPdf),
            document.CreatedAt,
            document.SubmittedAt,
            document.CompletedAt,
            document.Signers
                .OrderBy(static s => s.Order)
                .Select(s => new DocumentSignerItem(s.UserId, s.User!.FullName, positions.GetValueOrDefault(s.UserId), s.Order, s.Status, s.DecidedAt, s.Comment, s.User.PhotoId))
                .ToList(),
            document.Signatures
                .OrderBy(static s => s.SignedAt)
                .Select(s => new DocumentSignatureItem(s.Id, s.UserId, s.User!.FullName, s.Format, s.Packaging, s.Level, s.KeyMedia, s.Certificate, s.SignedAt, SignedFileName.For(document.File.FileName, s.Format), s.File.Size))
                .ToList(),
            actions,
            document.FirstSignature is { } first && SignatureChain.IsCumulative(first.Format) ? document.LatestSignature?.Id : null);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(CreateDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        if (!currentUser.Has(Permissions.Documents.Create))
        {
            return Error.Forbidden();
        }

        var contentType = FileRules.ResolveDocumentType(command.File.FileName);
        if (contentType is null)
        {
            return Error.Validation(Texts.T("Цей тип файлу не підтримується."));
        }

        if (command.File.Length > FileRules.MaxDocumentSize)
        {
            return Error.Validation(Texts.T("Розмір файлу не може перевищувати {0} МБ.", FileRules.MaxDocumentSize / 1024 / 1024));
        }

        var allowedUnits = await GetAuthorUnitsAsync(cancellationToken);
        if (!allowedUnits.Any(u => u.Id == command.OrganizationUnitId))
        {
            return Error.Validation(Texts.T("Оберіть підрозділ, до якого ви належите."));
        }

        var signerIds = command.SignerIds.Distinct().ToList();
        var activeSigners = await db.Users.CountAsync(u => signerIds.Contains(u.Id) && u.IsActive, cancellationToken);
        if (activeSigners != signerIds.Count)
        {
            return Error.Validation(Texts.T("Серед підписантів є неактивні або неіснуючі користувачі."));
        }

        var blob = await fileStorage.SaveAsync(command.File.Content, "documents", FileRules.MaxDocumentSize, cancellationToken);
        var saved = false;
        try
        {
            var result = await DomainErrors.GuardAsync<Guid>(async () =>
            {
                var number = await numbers.NextAsync(cancellationToken);
                var file = new StoredFile(command.File.FileName, contentType, blob.Size, blob.Sha256, blob.StorageKey);
                var document = new Document(number, command.Title, command.Description, command.Kind, me, command.OrganizationUnitId, command.SigningMode, file);
                document.SetRoute(signerIds, command.SigningMode);
                if (command.Submit)
                {
                    document.Submit(timeProvider.GetUtcNow());
                }

                db.Documents.Add(document);
                audit.Add(AuditActions.DocumentCreated, nameof(Document), document.Id.ToString(), $"{number} «{document.Title}»");
                await db.SaveChangesAsync(cancellationToken);
                return document.Id;
            });

            saved = result.IsSuccess;
            return result;
        }
        finally
        {
            if (!saved)
            {
                await fileStorage.DeleteAsync(blob.StorageKey, CancellationToken.None);
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentEditModel>> GetForEditAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var document = await db.Documents
            .AsNoTracking()
            .Include(static d => d.Signers)
            .Include(static d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null || !await IsVisibleAsync(id, cancellationToken))
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        if (document.AuthorId != me || !currentUser.Has(Permissions.Documents.Create))
        {
            return Error.Forbidden(Texts.T("Змінювати документ може лише його автор."));
        }

        if (!document.IsEditable)
        {
            return Error.Conflict(Texts.T("Змінювати можна лише чернетку або відхилений документ."));
        }

        var signerIds = document.Signers.OrderBy(static s => s.Order).Select(static s => s.UserId).ToList();
        var people = await db.Users
            .AsNoTracking()
            .Where(u => signerIds.Contains(u.Id))
            .Select(static u => new UserLookupItem(
                u.Id,
                u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty),
                u.Email!,
                StoredText.PositionInUnit(
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.Title).FirstOrDefault(),
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.Role!.Name).FirstOrDefault(),
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.OrganizationUnit!.Name).FirstOrDefault()),
                u.PhotoId))
            .ToDictionaryAsync(static u => u.Id, cancellationToken);
        var unit = await db.OrganizationUnits
            .AsNoTracking()
            .Where(u => u.Id == document.OrganizationUnitId)
            .Select(static u => new UnitOption(u.Id, u.Name, u.Code, u.Type!.IsOrganization, 0))
            .FirstAsync(cancellationToken);

        return new DocumentEditModel(
            document.Id,
            document.RegistrationNumber,
            document.Title,
            document.Description,
            document.Kind,
            document.Status,
            unit,
            document.SigningMode,
            [.. signerIds.Where(people.ContainsKey).Select(signerId => people[signerId])],
            new DocumentFileInfo(document.File.FileName, document.File.ContentType, document.File.Size, document.File.Sha256, document.File.IsPdf),
            [.. document.Signatures.Select(static s => s.UserId).Distinct()]);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(UpdateDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        if (!currentUser.Has(Permissions.Documents.Create))
        {
            return Error.Forbidden();
        }

        var document = await db.Documents
            .Include(static d => d.Signers)
            .Include(static d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);
        if (document is null || !await IsVisibleAsync(command.Id, cancellationToken))
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        if (document.AuthorId != me)
        {
            return Error.Forbidden(Texts.T("Змінювати документ може лише його автор."));
        }

        // The current unit stays allowed even if the author's positions changed since the document was created.
        if (command.OrganizationUnitId != document.OrganizationUnitId)
        {
            var allowedUnits = await GetAuthorUnitsAsync(cancellationToken);
            if (!allowedUnits.Any(u => u.Id == command.OrganizationUnitId))
            {
                return Error.Validation(Texts.T("Оберіть підрозділ, до якого ви належите."));
            }
        }

        var signerIds = command.SignerIds.Distinct().ToList();
        var activeSigners = await db.Users.CountAsync(u => signerIds.Contains(u.Id) && u.IsActive, cancellationToken);
        if (activeSigners != signerIds.Count)
        {
            return Error.Validation(Texts.T("Серед підписантів є неактивні або неіснуючі користувачі."));
        }

        if (command.Submit && signerIds.Count == 0)
        {
            return Error.Validation(Texts.T("Додайте щонайменше одного підписанта."));
        }

        StoredFile? newFile = null;
        if (command.File is { } upload)
        {
            var contentType = FileRules.ResolveDocumentType(upload.FileName);
            if (contentType is null)
            {
                return Error.Validation(Texts.T("Цей тип файлу не підтримується."));
            }

            if (upload.Length > FileRules.MaxDocumentSize)
            {
                return Error.Validation(Texts.T("Розмір файлу не може перевищувати {0} МБ.", FileRules.MaxDocumentSize / 1024 / 1024));
            }

            var blob = await fileStorage.SaveAsync(upload.Content, "documents", FileRules.MaxDocumentSize, cancellationToken);
            newFile = new StoredFile(upload.FileName, contentType, blob.Size, blob.Sha256, blob.StorageKey);
        }

        var previousFile = document.File;
        var removed = new List<DocumentSignature>();
        var result = await DomainErrors.GuardAsync(async () =>
        {
            document.UpdateDetails(command.Title, command.Description, command.Kind, command.OrganizationUnitId);
            removed.AddRange(document.SetRoute(signerIds, command.SigningMode));
            if (newFile is not null)
            {
                removed.AddRange(document.ReplaceFile(newFile));
            }

            var details = newFile is null
                ? $"{document.RegistrationNumber} «{document.Title}»"
                : $"{document.RegistrationNumber} «{document.Title}»: новий файл {newFile.FileName}";
            if (removed.Count > 0)
            {
                details += $", анульовано підписів: {removed.Count}";
            }

            audit.Add(AuditActions.DocumentUpdated, nameof(Document), document.Id.ToString(), details);
            if (command.Submit)
            {
                document.Submit(timeProvider.GetUtcNow());
                audit.Add(AuditActions.DocumentSubmitted, nameof(Document), document.Id.ToString(), document.RegistrationNumber);
            }

            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });

        // Only files that are no longer referenced are deleted, and only after the database agreed.
        if (result.IsSuccess)
        {
            if (newFile is not null)
            {
                await fileStorage.DeleteAsync(previousFile.StorageKey, CancellationToken.None);
            }

            foreach (var signature in removed)
            {
                await fileStorage.DeleteAsync(signature.File.StorageKey, CancellationToken.None);
            }
        }
        else if (newFile is not null)
        {
            await fileStorage.DeleteAsync(newFile.StorageKey, CancellationToken.None);
        }

        return result;
    }

    /// <inheritdoc />
    public Task<Result> SubmitAsync(Guid id, CancellationToken cancellationToken)
        => MutateAsAuthorAsync(id, document => document.Submit(timeProvider.GetUtcNow()), AuditActions.DocumentSubmitted, cancellationToken);

    /// <inheritdoc />
    public Task<Result> WithdrawAsync(Guid id, CancellationToken cancellationToken)
        => MutateAsAuthorAsync(id, static document => document.Withdraw(), AuditActions.DocumentWithdrawn, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<Guid>> AddSignatureAsync(AddSignatureCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        if (!currentUser.Has(Permissions.Documents.Sign))
        {
            return Error.Forbidden(Texts.T("У вас немає права підписувати документи."));
        }

        if (command.Signature.Length is <= 0 or > FileRules.MaxSignatureSize)
        {
            return Error.Validation(Texts.T("Некоректний розмір файлу підпису."));
        }

        var document = await db.Documents
            .Include(static d => d.Signers)
            .Include(static d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);
        if (document is null || !document.IsAwaitingSignatureFrom(me))
        {
            return document is null ? Error.NotFound(Texts.T("Документ не знайдено.")) : Error.Conflict(Texts.T("Документ зараз не очікує вашого підпису."));
        }

        var now = timeProvider.GetUtcNow();
        var certificate = command.ReportedCertificate;
        Stream content = command.Signature.Content;
        MemoryStream? buffer = null;
        var appended = command.BaseSignatureId is not null && SignatureChain.IsCumulative(command.Format);
        try
        {
            if (appended || (command.Format == SignatureFormat.CAdES && command.Signature.Length <= InspectableSignatureSize))
            {
                buffer = new MemoryStream((int)command.Signature.Length);
                await command.Signature.Content.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;
                content = buffer;
            }

            if (appended)
            {
                // The new file must be the previous version plus exactly one signature (the chain rules themselves are
                // enforced by Document.AddSignature, again after any reload).
                var previous = document.Signatures.FirstOrDefault(s => s.Id == command.BaseSignatureId);
                if (previous is null)
                {
                    return Error.Conflict(Texts.T("Поки ви підписували, документ підписав інший підписант. Підпишіть останню версію ще раз."));
                }

                var check = await CheckAppendedAsync(previous, buffer!.GetBuffer().AsMemory(0, (int)buffer.Length), command.Format, cancellationToken);
                if (!check.IsSuccess)
                {
                    return check.Error;
                }

                certificate = check.Value ?? certificate;
            }
            else if (buffer is not null)
            {
                certificate = signatureInspector.TryReadSigner(buffer.GetBuffer().AsMemory(0, (int)buffer.Length), command.Format) ?? certificate;
            }

            if (string.IsNullOrWhiteSpace(certificate.SubjectFullName) || string.IsNullOrWhiteSpace(certificate.SerialNumber))
            {
                return Error.Validation(Texts.T("Не вдалося визначити сертифікат підписувача."));
            }

            var signedAt = command.SignedAt is { } reported && reported <= now + AllowedClockSkew && reported >= (document.SubmittedAt ?? now) - AllowedClockSkew
                ? reported
                : now;

            var blob = await fileStorage.SaveAsync(content, "signatures", FileRules.MaxSignatureSize, cancellationToken);
            var contentType = SignatureContentType(command.Format);

            var result = await DomainErrors.GuardAsync<Guid>(async () =>
            {
                for (var attempt = 1; ; attempt++)
                {
                    try
                    {
                        var file = new StoredFile(SignedFileName.For(document.File.FileName, command.Format), contentType, blob.Size, blob.Sha256, blob.StorageKey);
                        var signature = document.AddSignature(me, command.Format, command.Packaging, command.Level, command.KeyMedia, file, certificate, signedAt, now, command.BaseSignatureId);
                        audit.Add(AuditActions.DocumentSigned, nameof(Document), document.Id.ToString(), $"{document.RegistrationNumber}: {command.Format}, {certificate.IssuerName}, № {certificate.SerialNumber}");
                        await db.SaveChangesAsync(cancellationToken);
                        return signature.Id;
                    }
                    catch (DbUpdateConcurrencyException) when (attempt < MaxSaveAttempts)
                    {
                        // Another signer of a parallel route (or the author) saved the document first. An independent
                        // (XAdES) signature is still valid and is attached again; a cumulative one was made on a version
                        // that is no longer the newest, and AddSignature asks to sign the newest one instead.
                        db.DiscardChanges();
                        document = await db.Documents
                            .Include(static d => d.Signers)
                            .Include(static d => d.Signatures)
                            .FirstOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);
                        if (document is null)
                        {
                            return Error.NotFound(Texts.T("Документ не знайдено."));
                        }
                    }
                }
            });

            if (!result.IsSuccess)
            {
                await fileStorage.DeleteAsync(blob.StorageKey, CancellationToken.None);
            }

            return result;
        }
        finally
        {
            if (buffer is not null)
            {
                await buffer.DisposeAsync();
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result<SigningSession>> BeginSigningAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        if (!currentUser.Has(Permissions.Documents.Sign))
        {
            return Error.Forbidden(Texts.T("У вас немає права підписувати документи."));
        }

        for (var attempt = 1; ; attempt++)
        {
            var document = await db.Documents
                .Include(static d => d.Signers)
                .Include(static d => d.Signatures)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
            if (document is null)
            {
                return Error.NotFound(Texts.T("Документ не знайдено."));
            }

            if (!document.IsAwaitingSignatureFrom(me))
            {
                return Error.Conflict(Texts.T("Документ зараз не очікує вашого підпису."));
            }

            var now = timeProvider.GetUtcNow();
            if (document.IsSigningTurnHeldByOther(me, now))
            {
                var holder = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == document.SigningTurnUserId, cancellationToken);
                return new Error(
                    SigningTurnErrorCode,
                    Texts.T("Документ зараз підписує {0}. Додати свій підпис можна буде, щойно він завершить.", holder?.FullName ?? Texts.T("інший підписант")),
                    ErrorType.Conflict);
            }

            try
            {
                var expiresAt = document.BeginSigning(me, now);
                await db.SaveChangesAsync(cancellationToken);

                var first = document.FirstSignature;
                var latest = first is not null && SignatureChain.IsCumulative(first.Format) ? document.LatestSignature : null;
                return new SigningSession(latest?.Id, latest?.File.Sha256, first?.Format, first?.Packaging, latest is null ? 0 : document.Signatures.Count, expiresAt);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxSaveAttempts)
            {
                // Someone signed or took the turn at the same moment: look again.
                db.DiscardChanges();
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result> EndSigningAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null || document.SigningTurnUserId != me)
        {
            return Result.Success();
        }

        document.EndSigning(me);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The turn ends on its own when it expires; nothing else depends on giving it up early.
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RejectAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var document = await db.Documents.Include(static d => d.Signers).FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            document.Reject(me, reason, timeProvider.GetUtcNow());
            audit.Add(AuditActions.DocumentRejected, nameof(Document), document.Id.ToString(), $"{document.RegistrationNumber}: {reason}");
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });
    }

    /// <inheritdoc />
    public async Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null || !await IsVisibleAsync(id, cancellationToken))
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        if (document.AuthorId != me && !currentUser.Has(Permissions.Documents.Manage))
        {
            return Error.Forbidden();
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            document.Archive();
            audit.Add(AuditActions.DocumentArchived, nameof(Document), document.Id.ToString(), document.RegistrationNumber);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var document = await db.Documents
            .Include(static d => d.Signers)
            .Include(static d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null || !await IsVisibleAsync(id, cancellationToken))
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        if (document.AuthorId != me && !currentUser.Has(Permissions.Documents.Manage))
        {
            return Error.Forbidden();
        }

        var result = await DomainErrors.GuardAsync(async () =>
        {
            document.EnsureCanBeDeleted();
            db.Documents.Remove(document);
            audit.Add(AuditActions.DocumentDeleted, nameof(Document), document.Id.ToString(), $"{document.RegistrationNumber} «{document.Title}»");
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });

        if (result.IsSuccess)
        {
            await fileStorage.DeleteAsync(document.File.StorageKey, CancellationToken.None);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<DownloadableFile>> OpenFileAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await IsVisibleAsync(id, cancellationToken))
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        var file = await db.Documents.AsNoTracking().Where(d => d.Id == id).Select(static d => d.File).FirstAsync(cancellationToken);
        var stream = await fileStorage.OpenReadAsync(file.StorageKey, cancellationToken);
        return new DownloadableFile(stream, file.FileName, file.ContentType);
    }

    /// <inheritdoc />
    public async Task<Result<DownloadableFile>> OpenSignatureAsync(Guid id, Guid signatureId, CancellationToken cancellationToken)
    {
        if (!await IsVisibleAsync(id, cancellationToken))
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        var signature = await db.DocumentSignatures
            .AsNoTracking()
            .Where(s => s.Id == signatureId && s.DocumentId == id)
            .Select(static s => new { s.File, s.Format })
            .FirstOrDefaultAsync(cancellationToken);
        if (signature is null)
        {
            return Error.NotFound(Texts.T("Підпис не знайдено."));
        }

        // Named after the document at download time, so signatures stored under older names download the same way.
        var documentName = await db.Documents.AsNoTracking().Where(d => d.Id == id).Select(static d => d.File.FileName).FirstAsync(cancellationToken);
        var stream = await fileStorage.OpenReadAsync(signature.File.StorageKey, cancellationToken);
        return new DownloadableFile(stream, SignedFileName.For(documentName, signature.Format), signature.File.ContentType);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitOption>> GetAuthorUnitsAsync(CancellationToken cancellationToken)
    {
        var units = await db.OrganizationUnits
            .AsNoTracking()
            .Where(static u => u.IsActive)
            .Select(static u => new { u.Id, u.ParentId, u.Name, u.Code, u.Type!.IsOrganization, u.SortOrder })
            .ToListAsync(cancellationToken);

        // Authors file documents under their own units; the organization or system-wide viewers may file anywhere they can see.
        IReadOnlySet<Guid>? allowed = currentUser.Has(Permissions.Documents.ViewAll)
            ? null
            : currentUser.Has(Permissions.Documents.ViewOrganization)
                ? await accessScope.GetOrganizationScopeAsync(cancellationToken)
                : await accessScope.GetUnitScopeAsync(cancellationToken);

        var children = units.ToLookup(static u => u.ParentId);
        var result = new List<UnitOption>(units.Count);
        void Walk(Guid? parentId, int depth)
        {
            foreach (var unit in children[parentId].OrderBy(static u => u.SortOrder).ThenBy(static u => u.Name, StringComparer.CurrentCulture))
            {
                if (allowed is null || allowed.Contains(unit.Id))
                {
                    result.Add(new UnitOption(unit.Id, unit.Name, unit.Code, unit.IsOrganization, depth));
                }

                Walk(unit.Id, depth + 1);
            }
        }

        Walk(null, 0);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserLookupItem>> SearchSignersAsync(string term, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Documents.Create) || string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return [];
        }

        var lowered = term.Trim();
        return await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && (u.LastName.Contains(lowered) || u.FirstName.Contains(lowered) || u.Email!.Contains(lowered)))
            .OrderBy(static u => u.LastName).ThenBy(static u => u.FirstName)
            .Take(10)
            .Select(static u => new UserLookupItem(
                u.Id,
                u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty),
                u.Email!,
                StoredText.PositionInUnit(
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.Title).FirstOrDefault(),
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.Role!.Name).FirstOrDefault(),
                    u.Positions.Where(static p => p.IsPrimary).Select(static p => p.OrganizationUnit!.Name).FirstOrDefault()),
                u.PhotoId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Maps a signature format to the MIME type of its file (the name comes from <see cref="SignedFileName"/>).
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>The MIME type.</returns>
    private static string SignatureContentType(SignatureFormat format) => format switch
    {
        SignatureFormat.XAdES => "application/xml",
        SignatureFormat.PAdES => "application/pdf",
        SignatureFormat.AsicS => "application/vnd.etsi.asic-s+zip",
        SignatureFormat.AsicE => "application/vnd.etsi.asic-e+zip",
        _ => "application/pkcs7-signature",
    };

    /// <summary>
    /// Checks that a cumulative signature file is the previous version with exactly one more signature, and finds the
    /// new signer's certificate in a CAdES container (the earlier signers are already there).
    /// </summary>
    /// <param name="previous">The signature the new one was added to.</param>
    /// <param name="signed">The new signature file.</param>
    /// <param name="format">The signature format.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new signer's certificate when it could be read, or a validation error.</returns>
    private async Task<Result<SignerCertificate?>> CheckAppendedAsync(DocumentSignature previous, ReadOnlyMemory<byte> signed, SignatureFormat format, CancellationToken cancellationToken)
    {
        await using var stream = await fileStorage.OpenReadAsync(previous.File.StorageKey, cancellationToken);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, cancellationToken);
        var before = copy.GetBuffer().AsMemory(0, (int)copy.Length);

        var invalid = Error.Validation(Texts.T("Файл підпису не містить попередніх підписів документа. Підпишіть останню версію ще раз."));
        var expected = signatureInspector.TryCountSignatures(before, format);
        var actual = signatureInspector.TryCountSignatures(signed, format);
        if (expected is { } count && actual is { } total && total != count + 1)
        {
            return invalid;
        }

        // A PDF signature is an incremental update: the earlier signed PDF stays byte for byte at the start.
        if (format == SignatureFormat.PAdES && !signed.Span.StartsWith(before.Span))
        {
            return invalid;
        }

        if (format != SignatureFormat.CAdES)
        {
            return Result<SignerCertificate?>.Success(null);
        }

        var earlier = signatureInspector.TryReadSigners(before, format) ?? [];
        var added = (signatureInspector.TryReadSigners(signed, format) ?? [])
            .Where(s => !earlier.Any(e => e.SerialNumber == s.SerialNumber && e.IssuerName == s.IssuerName))
            .ToList();
        return Result<SignerCertificate?>.Success(added.Count == 1 ? added[0] : null);
    }

    /// <summary>
    /// Checks whether the current user can see the document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when visible.</returns>
    private async Task<bool> IsVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Documents.View))
        {
            return false;
        }

        var visible = await accessScope.VisibleDocumentsAsync(db.Documents.AsNoTracking().Where(d => d.Id == id), cancellationToken);
        return await visible.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Loads a document as its author, applies a state transition and saves it.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="mutation">The transition.</param>
    /// <param name="auditAction">The audit action code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    private async Task<Result> MutateAsAuthorAsync(Guid id, Action<Document> mutation, string auditAction, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } me)
        {
            return Error.Unauthorized();
        }

        var document = await db.Documents
            .Include(static d => d.Signers)
            .Include(static d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (document is null)
        {
            return Error.NotFound(Texts.T("Документ не знайдено."));
        }

        if (document.AuthorId != me)
        {
            return Error.Forbidden(Texts.T("Цю дію може виконати лише автор документа."));
        }

        return await DomainErrors.GuardAsync(async () =>
        {
            mutation(document);
            audit.Add(auditAction, nameof(Document), document.Id.ToString(), document.RegistrationNumber);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        });
    }
}
