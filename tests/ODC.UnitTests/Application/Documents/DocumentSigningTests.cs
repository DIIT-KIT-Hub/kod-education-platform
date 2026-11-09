using System.Text;
using ODC.UnitTests.Infrastructure;
using ODC.Web.Features.Documents;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.Notifications;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application.Documents;

/// <summary>
/// The signing turn, the signature chain, declining and the notifications of the route.
/// </summary>
/// <param name="database">The PostgreSQL test database of the class.</param>
[Collection(PostgresCollection.Name)]
public sealed class DocumentSigningTests(PostgresDatabase database) : DocumentTestBase(database)
{
    [Fact]
    public async Task The_signer_whose_turn_it_is_takes_the_signing_turn_and_others_wait()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);

        var session = (await World.As(World.SignerA).SendAsync(new SigningTurn.BeginCommand(id))).Ok();
        Assert.Equal(DocumentWorld.Start + SignatureChain.TurnDuration, session.TurnExpiresAt);
        Assert.Null(session.BaseSignatureId);

        var waiting = await World.As(World.SignerA2).SendAsync(new SigningTurn.BeginCommand(id));
        Assert.Equal(ErrorType.Conflict, waiting.Failure());
        Assert.Equal("signing_turn", waiting.Error?.Code);
        Assert.Contains("Бондар Богдан", waiting.Error?.Message, StringComparison.Ordinal);

        // The same signer renews the turn; once it is given up the other signer may take it.
        Assert.True((await World.As(World.SignerA).SendAsync(new SigningTurn.BeginCommand(id))).IsSuccess);
        Assert.True((await World.As(World.SignerA).SendAsync(new SigningTurn.EndCommand(id))).IsSuccess);
        Assert.True((await World.As(World.SignerA2).SendAsync(new SigningTurn.BeginCommand(id))).IsSuccess);
    }

    [Fact]
    public async Task A_forgotten_turn_expires_after_ten_minutes()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);
        (await World.As(World.SignerA).SendAsync(new SigningTurn.BeginCommand(id))).Ok();

        World.Clock.Now += SignatureChain.TurnDuration + TimeSpan.FromSeconds(1);

        Assert.True((await World.As(World.SignerA2).SendAsync(new SigningTurn.BeginCommand(id))).IsSuccess);
    }

    [Fact]
    public async Task Only_a_signer_whose_turn_it_is_may_take_the_turn()
    {
        var id = await CreateAsync(submit: true);

        Assert.Equal(ErrorType.Conflict, (await World.As(World.SignerA2).SendAsync(new SigningTurn.BeginCommand(id))).Failure());
        Assert.Equal(ErrorType.Conflict, (await World.As(World.AuthorA).SendAsync(new SigningTurn.BeginCommand(id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerA).SendAsync(new SigningTurn.BeginCommand(Guid.NewGuid()))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await World.As(World.SignerA, permissions: new HashSet<string> { "documents.view" }).SendAsync(new SigningTurn.BeginCommand(id))).Failure());
    }

    [Fact]
    public async Task Giving_up_a_turn_one_does_not_hold_changes_nothing()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);
        (await World.As(World.SignerA).SendAsync(new SigningTurn.BeginCommand(id))).Ok();

        Assert.True((await World.As(World.SignerA2).SendAsync(new SigningTurn.EndCommand(id))).IsSuccess);
        Assert.True((await World.As(World.SignerA2).SendAsync(new SigningTurn.EndCommand(Guid.NewGuid()))).IsSuccess);
        Assert.Equal(ErrorType.Conflict, (await World.As(World.SignerA2).SendAsync(new SigningTurn.BeginCommand(id))).Failure());
    }

    [Fact]
    public async Task A_sequential_route_is_signed_in_order_and_the_author_hears_when_it_is_complete()
    {
        var id = await CreateAsync(submit: true);

        Assert.Equal(ErrorType.Conflict, (await SignAsync(World.SignerA2, id)).Failure());
        var first = (await SignAsync(World.SignerA, id)).Ok();
        Assert.Contains(World.NotificationEvents.Received, r => r.UserId == World.SignerA2.Id && r.Notification.Kind == NotificationKind.DocumentToSign);

        var session = (await World.As(World.SignerA2).SendAsync(new SigningTurn.BeginCommand(id))).Ok();
        Assert.Equal(first, session.BaseSignatureId);
        Assert.Equal(1, session.SignatureCount);
        Assert.Equal(SignatureFormat.CAdES, session.Format);

        (await SignAsync(World.SignerA2, id)).Ok();

        var document = await GetAsync(World.AuthorA, id);
        Assert.Equal(DocumentStatus.Signed, document.Status);
        Assert.NotNull(document.CompletedAt);
        Assert.All(document.Signers, static s => Assert.Equal(SignerStatus.Signed, s.Status));
        Assert.Equal(2, document.Signatures.Count);
        Assert.NotNull(document.CombinedSignatureId);
        Assert.Contains(World.NotificationEvents.Received, r => r.UserId == World.AuthorA.Id && r.Notification.Kind == NotificationKind.DocumentSigned);
        Assert.Equal(2, World.Audit.Actions().Count(static a => a == AuditActions.DocumentSigned));
        Assert.Equal(2, World.Files.In("signatures").Count);
    }

    [Fact]
    public async Task The_certificate_is_taken_from_the_signature_file_not_from_the_browser()
    {
        var id = await CreateAsync(submit: true, signers: [World.SignerA]);

        (await SignAsync(World.SignerA, id, serials: ["REAL-SERIAL"])).Ok();

        var signature = Assert.Single((await GetAsync(World.AuthorA, id)).Signatures);
        Assert.Equal("REAL-SERIAL", signature.Certificate.SerialNumber);
    }

    [Fact]
    public async Task A_cumulative_signature_must_be_the_previous_version_plus_exactly_one_signature()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);
        (await SignAsync(World.SignerA, id)).Ok();

        var alone = await SignAsync(World.SignerA2, id, serials: [World.SignerA2.Email!]);
        var twoNew = await SignAsync(World.SignerA2, id, serials: [World.SignerA.Email!, "X", World.SignerA2.Email!]);

        Assert.Equal(ErrorType.Validation, alone.Failure());
        Assert.Equal(ErrorType.Validation, twoNew.Failure());
        Assert.Single(World.Files.In("signatures"));
        Assert.Equal(DocumentStatus.InSigning, (await GetAsync(World.AuthorA, id)).Status);
    }

    [Fact]
    public async Task A_signature_on_an_unknown_previous_version_is_refused()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);
        (await SignAsync(World.SignerA, id)).Ok();

        var stale = await SignAsync(World.SignerA2, id, baseSignatureId: Guid.NewGuid());

        Assert.Equal(ErrorType.Conflict, stale.Failure());
    }

    [Fact]
    public async Task A_cumulative_signature_without_the_newest_version_asks_to_sign_it_again()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);
        (await SignAsync(World.SignerA, id)).Ok();

        // The browser did not know about the first signature: a new chain would drop it.
        var bytes = DocumentWorld.ScriptedSignatureInspector.File(World.SignerA2.Email!);
        var result = await World.As(World.SignerA2).SendAsync(new AddSignatureCommand(
            id,
            SignatureFormat.CAdES,
            SignaturePackaging.Detached,
            "B-LT",
            KeyMediaType.File,
            null,
            DocumentWorld.ScriptedSignatureInspector.Certificate(World.SignerA2.Email!),
            new FileUpload(new MemoryStream(bytes), "s.p7s", "application/pkcs7-signature", bytes.Length),
            null));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Empty(World.Files.In("signatures").Skip(1));
    }

    [Fact]
    public async Task Independent_XAdES_signatures_need_no_previous_version()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);

        (await SignAsync(World.SignerA, id, SignatureFormat.XAdES)).Ok();
        (await SignAsync(World.SignerA2, id, SignatureFormat.XAdES)).Ok();

        var document = await GetAsync(World.AuthorA, id);
        Assert.Equal(DocumentStatus.Signed, document.Status);
        Assert.Null(document.CombinedSignatureId);
    }

    [Fact]
    public async Task Every_signer_follows_the_format_of_the_first_signature()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);
        (await SignAsync(World.SignerA, id, SignatureFormat.XAdES)).Ok();

        Assert.Equal(ErrorType.Validation, (await SignAsync(World.SignerA2, id, SignatureFormat.CAdES, serials: [World.SignerA2.Email!])).Failure());
    }

    [Fact]
    public async Task PAdES_is_offered_only_for_PDF_documents_and_appends_to_the_signed_PDF()
    {
        var pdf = await CreateAsync(submit: true, mode: SigningMode.Parallel);
        var docx = (await World.As(World.AuthorA).SendAsync(Create([World.SignerA], submit: true, file: Upload("order.docx", "docx")))).Ok();

        Assert.Equal(ErrorType.Validation, (await SignAsync(World.SignerA, docx, SignatureFormat.PAdES)).Failure());
        (await SignAsync(World.SignerA, pdf, SignatureFormat.PAdES)).Ok();

        // The next PDF must start with the previous signed PDF byte for byte (an incremental update).
        Assert.Equal(ErrorType.Validation, (await SignAsync(World.SignerA2, pdf, SignatureFormat.PAdES, serials: ["Z", World.SignerA2.Email!])).Failure());
        (await SignAsync(World.SignerA2, pdf, SignatureFormat.PAdES)).Ok();
        Assert.Equal(DocumentStatus.Signed, (await GetAsync(World.AuthorA, pdf)).Status);
    }

    [Fact]
    public async Task A_signature_file_must_have_a_sensible_size_and_the_signer_must_hold_the_permission()
    {
        var id = await CreateAsync(submit: true, signers: [World.SignerA]);
        var empty = new AddSignatureCommand(
            id,
            SignatureFormat.CAdES,
            SignaturePackaging.Detached,
            null,
            KeyMediaType.File,
            null,
            DocumentWorld.ScriptedSignatureInspector.Certificate("1"),
            new FileUpload(new MemoryStream(), "s.p7s", "application/pkcs7-signature", 0),
            null);

        Assert.Equal(ErrorType.Validation, (await World.As(World.SignerA).SendAsync(empty)).Failure());
        Assert.Equal(ErrorType.Validation, (await World.As(World.SignerA).SendAsync(empty with { Signature = new FileUpload(new MemoryStream([1]), "s", "x", (40L * 1024 * 1024) + 1) })).Failure());
        Assert.Equal(ErrorType.Forbidden, (await World.As(World.SignerA, permissions: new HashSet<string> { "documents.view" }).SendAsync(empty)).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerA).SendAsync(empty with { DocumentId = Guid.NewGuid(), Signature = Upload("s.p7s", "SIG:1") })).Failure());
    }

    [Fact]
    public async Task A_signature_without_a_readable_certificate_is_refused()
    {
        var id = await CreateAsync(submit: true, signers: [World.SignerA]);
        var bytes = Encoding.UTF8.GetBytes("not a signature");

        var result = await World.As(World.SignerA).SendAsync(new AddSignatureCommand(
            id,
            SignatureFormat.XAdES,
            SignaturePackaging.Detached,
            null,
            KeyMediaType.File,
            null,
            new SignerCertificate(" ", null, null, "КНЕДП", " ", null, null),
            new FileUpload(new MemoryStream(bytes), "s.xml", "application/xml", bytes.Length),
            null));

        Assert.Equal(ErrorType.Validation, result.Failure());
    }

    [Fact]
    public async Task A_signing_time_reported_by_the_browser_is_kept_only_when_plausible()
    {
        var id = await CreateAsync(submit: true, mode: SigningMode.Parallel);

        await SendSignatureAt(World.SignerA, id, DocumentWorld.Start.AddMinutes(-2));
        await SendSignatureAt(World.SignerA2, id, DocumentWorld.Start.AddYears(1));

        var signatures = (await GetAsync(World.AuthorA, id)).Signatures;
        Assert.Equal(DocumentWorld.Start.AddMinutes(-2), signatures.Single(s => s.UserId == World.SignerA.Id).SignedAt);
        Assert.Equal(DocumentWorld.Start, signatures.Single(s => s.UserId == World.SignerA2.Id).SignedAt);
    }

    [Fact]
    public async Task A_signer_declines_with_a_reason_and_the_author_hears_it()
    {
        var id = await CreateAsync(submit: true);

        Assert.Equal(ErrorType.Validation, (await World.As(World.SignerA2).SendAsync(new RejectDocument.Command(id, "Не моя черга"))).Failure());
        Assert.Equal(ErrorType.Validation, (await World.As(World.SignerA).SendAsync(new RejectDocument.Command(id, " "))).Failure());
        Assert.True((await World.As(World.SignerA).SendAsync(new RejectDocument.Command(id, "Невірні дати"))).IsSuccess);

        var document = await GetAsync(World.AuthorA, id);
        Assert.Equal(DocumentStatus.Rejected, document.Status);
        Assert.Equal("Невірні дати", document.Signers.Single(s => s.UserId == World.SignerA.Id).Comment);
        var told = Assert.Single(World.NotificationEvents.Received, r => r.Notification.Kind == NotificationKind.DocumentRejected);
        Assert.Equal(World.AuthorA.Id, told.UserId);
        Assert.Equal("Невірні дати", told.Notification.Preview);
        Assert.Contains(AuditActions.DocumentRejected, World.Audit.Actions());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerA).SendAsync(new RejectDocument.Command(Guid.NewGuid(), "x"))).Failure());
    }

    [Fact]
    public async Task A_corrected_document_goes_to_signing_again_and_earlier_signatures_on_the_same_file_stay()
    {
        var id = await CreateAsync(submit: true);
        (await SignAsync(World.SignerA, id)).Ok();
        await World.As(World.SignerA2).SendAsync(new RejectDocument.Command(id, "Додайте додаток"));

        var resend = await World.As(World.AuthorA).SendAsync(new UpdateDocumentCommand(
            id, "Наказ про відпустки", "З додатком", DocumentKind.Order, World.World.AHr.Id, SigningMode.Sequential, [World.SignerA.Id, World.SignerA2.Id], true, null));

        Assert.True(resend.IsSuccess);
        var document = await GetAsync(World.AuthorA, id);
        Assert.Equal(DocumentStatus.InSigning, document.Status);
        Assert.Equal(SignerStatus.Signed, document.Signers.Single(s => s.UserId == World.SignerA.Id).Status);
        Assert.Equal(SignerStatus.Pending, document.Signers.Single(s => s.UserId == World.SignerA2.Id).Status);
    }

    [Fact]
    public async Task A_new_file_for_a_declined_document_drops_its_signatures_and_their_files()
    {
        var id = await CreateAsync(submit: true);
        (await SignAsync(World.SignerA, id)).Ok();
        var signatureKey = World.Files.In("signatures").Single();
        await World.As(World.SignerA2).SendAsync(new RejectDocument.Command(id, "Новий файл"));

        var result = await World.As(World.AuthorA).SendAsync(new UpdateDocumentCommand(
            id, "Наказ про відпустки", null, DocumentKind.Order, World.World.AHr.Id, SigningMode.Sequential, [World.SignerA.Id, World.SignerA2.Id], false, Upload("v2.pdf", "%PDF v2")));

        Assert.True(result.IsSuccess);
        Assert.Empty((await GetAsync(World.AuthorA, id)).Signatures);
        Assert.Contains(signatureKey, World.Files.Deleted);
        Assert.Contains(World.Audit.Entries, e => e.Action == AuditActions.DocumentUpdated && e.Details!.Contains("анульовано підписів: 1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Opening_a_document_reads_the_notifications_about_it()
    {
        var id = await CreateAsync(submit: true);
        Assert.Equal(1, await World.As(World.SignerA).SendAsync(new CountUnreadNotifications.Query()));

        var document = await GetAsync(World.SignerA, id);

        Assert.True(document.Actions.CanSign);
        Assert.True(document.Actions.CanReject);
        Assert.Equal(0, await World.As(World.SignerA).SendAsync(new CountUnreadNotifications.Query()));
    }

    [Fact]
    public async Task Signing_reads_the_signers_own_notification_about_the_document()
    {
        var id = await CreateAsync(submit: true, signers: [World.SignerA]);

        (await SignAsync(World.SignerA, id)).Ok();

        Assert.Equal(0, await World.As(World.SignerA).SendAsync(new CountUnreadNotifications.Query()));
    }

    [Fact]
    public async Task Signature_files_are_downloaded_only_by_those_who_see_the_document()
    {
        var id = await CreateAsync(submit: true, signers: [World.SignerA]);
        var signatureId = (await SignAsync(World.SignerA, id)).Ok();

        var file = (await World.As(World.AuthorA).SendAsync(new DownloadDocumentFiles.SignatureQuery(id, signatureId))).Ok();
        using var reader = new StreamReader(file.Content);
        Assert.StartsWith("SIG:", await reader.ReadToEndAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.EndsWith(".p7s", file.FileName, StringComparison.Ordinal);

        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerB).SendAsync(new DownloadDocumentFiles.SignatureQuery(id, signatureId))).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.AuthorA).SendAsync(new DownloadDocumentFiles.SignatureQuery(id, Guid.NewGuid()))).Failure());
    }

    /// <summary>
    /// Sends an independent signature with a reported signing time.
    /// </summary>
    /// <param name="signer">The signer.</param>
    /// <param name="id">The document.</param>
    /// <param name="signedAt">The time the browser reports.</param>
    /// <returns>A task that completes when signed.</returns>
    private async Task SendSignatureAt(ODC.Web.Shared.Domain.Users.User signer, Guid id, DateTimeOffset signedAt)
    {
        var bytes = DocumentWorld.ScriptedSignatureInspector.File(signer.Email!);
        (await World.As(signer).SendAsync(new AddSignatureCommand(
            id,
            SignatureFormat.XAdES,
            SignaturePackaging.Detached,
            null,
            KeyMediaType.File,
            signedAt,
            DocumentWorld.ScriptedSignatureInspector.Certificate(signer.Email!),
            new FileUpload(new MemoryStream(bytes), "s.xml", "application/xml", bytes.Length),
            null))).Ok();
    }
}
