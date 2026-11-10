using Microsoft.EntityFrameworkCore;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Infrastructure.Tenancy;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Auditing;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Models;

namespace ODC.Web.Features.Documents;

/// <summary>
/// What the document slices share: who sees a document (<see cref="IAccessScope"/>), the units an author may file
/// under, the notifications about the progress of signing, author-only state changes and the check of a cumulative
/// signature file.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="catalog">The shared catalogs (which unit types are organizations).</param>
/// <param name="currentUser">The current user.</param>
/// <param name="accessScope">The data-level access rules.</param>
/// <param name="fileStorage">The private file storage.</param>
/// <param name="signatureInspector">The signature container inspector.</param>
/// <param name="audit">The audit logger.</param>
/// <param name="participants">Admits signers from other organizations.</param>
internal sealed class DocumentDesk(
    OrganizationDbContext db,
    Catalog catalog,
    ICurrentUser currentUser,
    IAccessScope accessScope,
    IFileStorage fileStorage,
    ISignatureInspector signatureInspector,
    IAuditLogger audit,
    ExternalParticipants participants)
{
    /// <summary>
    /// How many times a change is saved again when concurrent signers of a parallel route keep saving first.
    /// </summary>
    public const int MaxSaveAttempts = 3;

    /// <summary>
    /// The route group <c>/api/v1/documents</c> (people who see documents).
    /// </summary>
    /// <param name="api">The API group.</param>
    /// <returns>The group.</returns>
    public static RouteGroupBuilder MapDocuments(IEndpointRouteBuilder api)
        => api.MapGroup("documents").RequirePermission(Permissions.Documents.View).WithTags("Documents");

    /// <summary>
    /// The error of a document the user cannot see.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error NotFound() => Error.NotFound(Texts.T("Документ не знайдено."));

    /// <summary>
    /// The error of a user without <c>documents.sign</c>.
    /// </summary>
    /// <returns>The error.</returns>
    public static Error CannotSign() => Error.Forbidden(Texts.T("У Вас немає права підписувати документи."));

    /// <summary>
    /// Maps a signature format to the MIME type of its file (the name comes from <see cref="SignedFileName"/>).
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>The MIME type.</returns>
    public static string SignatureContentType(SignatureFormat format) => format switch
    {
        SignatureFormat.XAdES => "application/xml",
        SignatureFormat.PAdES => "application/pdf",
        SignatureFormat.AsicS => "application/vnd.etsi.asic-s+zip",
        SignatureFormat.AsicE => "application/vnd.etsi.asic-e+zip",
        _ => "application/pkcs7-signature",
    };

    /// <summary>
    /// Checks whether the current user can see the document.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when visible.</returns>
    public async Task<bool> IsVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUser.Has(Permissions.Documents.View))
        {
            return false;
        }

        var visible = await accessScope.VisibleDocumentsAsync(db.Documents.AsNoTracking().Where(d => d.Id == id), cancellationToken);
        return await visible.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Checks the signers of a route: active members of the organization or people of other organizations who sign
    /// documents in their own (their profiles are copied here, see <see cref="ExternalParticipants"/>).
    /// </summary>
    /// <param name="signerIds">The signers (distinct).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An error, or <see langword="null"/> when every signer may sign.</returns>
    public async Task<Error?> CheckSignersAsync(List<Guid> signerIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signerIds);
        await participants.AdmitAsync(signerIds, Permissions.Documents.Sign, cancellationToken);
        var active = await db.Members.CountAsync(u => signerIds.Contains(u.Id) && u.IsActive, cancellationToken);
        return active == signerIds.Count ? null : Error.Validation(Texts.T("Серед підписантів є неактивні або неіснуючі користувачі."));
    }

    /// <summary>
    /// Returns the units the current user may register documents in, in tree order: authors file documents under their
    /// own units; the organization or system-wide viewers anywhere they can see.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The units.</returns>
    public async Task<IReadOnlyList<UnitOption>> GetAuthorUnitsAsync(CancellationToken cancellationToken)
    {
        var organizationTypes = await catalog.OrganizationTypeIdsAsync(cancellationToken);
        var units = await db.OrganizationUnits
            .AsNoTracking()
            .Where(static u => u.IsActive)
            .Select(u => new { u.Id, u.ParentId, u.Name, u.Code, IsOrganization = organizationTypes.Contains(u.TypeId), u.SortOrder })
            .ToListAsync(cancellationToken);

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

    /// <summary>
    /// Checks that a cumulative signature file is the previous version with exactly one more signature, and finds the
    /// new signer's certificate in a CAdES container (the earlier signers are already there).
    /// </summary>
    /// <param name="previous">The signature the new one was added to.</param>
    /// <param name="signed">The new signature file.</param>
    /// <param name="format">The signature format.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new signer's certificate when it could be read, or a validation error.</returns>
    public async Task<Result<SignerCertificate?>> CheckAppendedAsync(DocumentSignature previous, ReadOnlyMemory<byte> signed, SignatureFormat format, CancellationToken cancellationToken)
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
    /// Loads a document as its author, applies a state transition and saves it.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="mutation">The transition.</param>
    /// <param name="auditAction">The audit action code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result.</returns>
    public async Task<Result> MutateAsAuthorAsync(Guid id, Action<Document> mutation, string auditAction, CancellationToken cancellationToken)
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
            return NotFound();
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
