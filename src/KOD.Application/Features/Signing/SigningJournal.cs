using KOD.Application.Abstractions;
using KOD.Application.Common.Results;
using KOD.Domain.Access;
using KOD.Domain.Audit;
using KOD.Domain.Localization;

namespace KOD.Application.Features.Signing;

/// <summary>
/// Default <see cref="ISigningJournal"/>.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The current user.</param>
/// <param name="audit">The audit logger.</param>
internal sealed class SigningJournal(IApplicationDbContext db, ICurrentUser currentUser, IAuditLogger audit) : ISigningJournal
{
    /// <summary>
    /// The most files one signature may cover (the signing tool allows fewer).
    /// </summary>
    private const int MaxFiles = 50;

    /// <summary>
    /// The longest file name kept in the log.
    /// </summary>
    private const int MaxNameLength = 255;

    /// <inheritdoc />
    public async Task<Result> RecordFilesSignedAsync(FileSigningRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!currentUser.Has(Permissions.Signing.Use))
        {
            return Error.Forbidden();
        }

        var names = record.FileNames
            .Select(static n => Path.GetFileName(n?.Trim() ?? string.Empty))
            .Where(static n => n.Length > 0)
            .Select(static n => n.Length > MaxNameLength ? n[..MaxNameLength] : n)
            .ToList();
        if (names.Count is 0 or > MaxFiles)
        {
            return Error.Validation(Texts.T("Некоректний перелік підписаних файлів."));
        }

        // The details are stored as written (like every audit entry); the certificate data identifies the key used.
        var parameters = string.IsNullOrWhiteSpace(record.Level) ? record.Format.ToString() : $"{record.Format} {record.Level.Trim()}";
        var signer = string.Join(", ", new[] { record.SignerName, record.Issuer, record.CertificateSerial is { } serial ? $"№ {serial}" : null }
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(static p => p!.Trim()));
        audit.Add(AuditActions.FilesSigned, details: $"{string.Join(", ", names)}: {parameters}{(signer.Length > 0 ? $"; {signer}" : string.Empty)}");
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
