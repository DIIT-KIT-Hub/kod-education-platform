using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Identity;

namespace ODC.Web.Features.Notifications.Reactions;

/// <summary>
/// Tells the signers whose turn came that a document waits for them (once per sending for signing), and the author when
/// the document got every signature or a signer declined it.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="currentUser">The one who acted.</param>
/// <param name="notifier">Creates the notifications.</param>
internal sealed class DocumentNotifications(OrganizationDbContext db, ICurrentUser currentUser, INotifier notifier)
    : IDomainEventHandler<DocumentSigningProgressed>
{
    /// <inheritdoc />
    public async Task HandleAsync(DocumentSigningProgressed domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var document = await db.Documents.AsNoTracking().Include(static d => d.Signers).FirstOrDefaultAsync(d => d.Id == domainEvent.DocumentId, cancellationToken);
        if (document is null)
        {
            return;
        }

        // The one who acted has seen the document: their own notifications about it are done.
        if (currentUser.Id is { } me)
        {
            await notifier.MarkReadAsync(me, document.Id, cancellationToken);
        }

        var subject = $"{document.RegistrationNumber} · {document.Title}";
        var link = $"/documents/{document.Id}";
        switch (document.Status)
        {
            case DocumentStatus.InSigning:
                var waiting = document.Signers.Where(s => document.IsAwaitingSignatureFrom(s.UserId)).Select(static s => s.UserId).ToList();
                var toSign = new NotificationDraft(NotificationKind.DocumentToSign, currentUser.Id, document.Id, subject, document.Description, link) { OnceSince = document.SubmittedAt };
                await notifier.NotifyAsync(toSign, waiting, cancellationToken);
                break;
            case DocumentStatus.Signed:
                var signed = new NotificationDraft(NotificationKind.DocumentSigned, currentUser.Id, document.Id, subject, null, link) { OnceSince = document.CompletedAt };
                await notifier.NotifyAsync(signed, [document.AuthorId], cancellationToken);
                break;
            case DocumentStatus.Rejected:
                var reason = document.Signers.Where(static s => s.Status == SignerStatus.Rejected).MaxBy(static s => s.DecidedAt)?.Comment;
                var rejected = new NotificationDraft(NotificationKind.DocumentRejected, currentUser.Id, document.Id, subject, reason, link) { OnceSince = document.CompletedAt };
                await notifier.NotifyAsync(rejected, [document.AuthorId], cancellationToken);
                break;
        }
    }
}
