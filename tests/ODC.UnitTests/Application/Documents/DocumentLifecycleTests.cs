using ODC.UnitTests.Infrastructure;
using ODC.Web.Features.Documents;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application.Documents;

/// <summary>
/// Creating, changing, sending, withdrawing, archiving and deleting documents.
/// </summary>
/// <param name="database">The PostgreSQL test database of the class.</param>
[Collection(PostgresCollection.Name)]
public sealed class DocumentLifecycleTests(PostgresDatabase database) : DocumentTestBase(database)
{
    [Fact]
    public async Task A_draft_gets_a_number_stores_the_file_with_its_digest_and_tells_nobody()
    {
        var id = await CreateAsync(submit: false);

        var document = await GetAsync(World.AuthorA, id);
        Assert.Equal("2026/000001", document.RegistrationNumber);
        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.Equal("order.pdf", document.File.FileName);
        Assert.True(document.File.IsPdf);
        Assert.Equal(64, document.File.Sha256.Length);
        Assert.Single(World.Files.In("documents"));
        Assert.Equal([World.SignerA.Id, World.SignerA2.Id], document.Signers.Select(static s => s.UserId));
        Assert.Contains(AuditActions.DocumentCreated, World.Audit.Actions());
        Assert.Empty(World.NotificationEvents.Received);

        Assert.True(document.Actions.CanEdit);
        Assert.True(document.Actions.CanSubmit);
        Assert.True(document.Actions.CanDelete);
        Assert.False(document.Actions.CanWithdraw);
        Assert.False(document.Actions.CanSign);
    }

    [Fact]
    public async Task A_document_sent_at_once_tells_the_first_signer_of_a_sequential_route_only()
    {
        var id = await CreateAsync(submit: true);

        Assert.Equal(DocumentStatus.InSigning, (await GetAsync(World.AuthorA, id)).Status);
        var told = Assert.Single(World.NotificationEvents.Received);
        Assert.Equal(World.SignerA.Id, told.UserId);
        Assert.Equal(NotificationKind.DocumentToSign, told.Notification.Kind);
    }

    [Fact]
    public async Task A_parallel_route_tells_every_signer_at_once()
    {
        await CreateAsync(submit: true, mode: SigningMode.Parallel);

        Assert.Equal(
            new[] { World.SignerA.Id, World.SignerA2.Id }.Order(),
            World.NotificationEvents.Received.Where(static r => r.Notification.Kind == NotificationKind.DocumentToSign).Select(static r => r.UserId).Order());
    }

    [Theory]
    [InlineData("run.exe")]
    [InlineData("noextension")]
    public async Task An_unsupported_file_type_is_refused_before_anything_is_stored(string name)
    {
        var result = await World.As(World.AuthorA).SendAsync(Create([World.SignerA], file: Upload(name)));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Empty(World.Files.Stored);
    }

    [Fact]
    public async Task A_file_over_25_megabytes_is_refused()
    {
        var result = await World.As(World.AuthorA).SendAsync(Create([World.SignerA], file: Upload(length: (25L * 1024 * 1024) + 1)));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Empty(World.Files.Stored);
    }

    [Fact]
    public async Task The_author_registers_documents_only_in_units_of_their_positions()
    {
        var result = await World.As(World.AuthorA).SendAsync(Create([World.SignerA], unit: World.World.BProj.Id));

        Assert.Equal(ErrorType.Validation, result.Failure());
    }

    [Fact]
    public async Task Signers_must_be_existing_active_users()
    {
        World.SignerA2.SetActive(false);
        World.Db.SaveChanges();

        var inactive = await World.As(World.AuthorA).SendAsync(Create([World.SignerA, World.SignerA2]));
        var unknown = await World.As(World.AuthorA).SendAsync(new CreateDocumentCommand("Наказ", null, DocumentKind.Order, World.World.AHr.Id, SigningMode.Sequential, [Guid.NewGuid()], false, Upload()));

        Assert.Equal(ErrorType.Validation, inactive.Failure());
        Assert.Equal(ErrorType.Validation, unknown.Failure());
    }

    [Fact]
    public async Task A_rule_of_the_domain_refuses_the_document_and_the_stored_file_is_removed()
    {
        var noTitle = await World.As(World.AuthorA).SendAsync(Create([World.SignerA], title: "  "));
        var noSigners = await World.As(World.AuthorA).SendAsync(Create([], submit: true));

        Assert.Equal(ErrorType.Validation, noTitle.Failure());
        Assert.Equal(ErrorType.Validation, noSigners.Failure());
        Assert.Empty(World.Files.Stored);
        Assert.Equal(2, World.Files.Deleted.Count);
        Assert.Empty(World.Db.Documents);
    }

    [Fact]
    public async Task A_broken_registration_number_is_a_validation_error_and_the_file_is_removed()
    {
        World.Numbers.IssueInvalid = true;

        var result = await World.As(World.AuthorA).SendAsync(Create([World.SignerA]));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Empty(World.Files.Stored);
    }

    [Fact]
    public async Task Only_users_allowed_to_create_documents_can_create_them()
    {
        var result = await World.As(World.ReaderA).SendAsync(Create([World.SignerA]));

        Assert.Equal(ErrorType.Forbidden, result.Failure());
        Assert.Empty(World.Files.Stored);
    }

    [Fact]
    public async Task The_author_changes_a_draft_replaces_its_file_and_the_old_file_is_removed()
    {
        var id = await CreateAsync(submit: false);
        var oldKey = World.Files.In("documents").Single();

        var result = await World.As(World.AuthorA).SendAsync(new UpdateDocumentCommand(
            id, "Наказ (нова редакція)", "Нова редакція", DocumentKind.Memo, World.World.AHr.Id, SigningMode.Parallel, [World.SignerA2.Id], false, Upload("order.docx", "new")));

        Assert.True(result.IsSuccess);
        var document = await GetAsync(World.AuthorA, id);
        Assert.Equal("Наказ (нова редакція)", document.Title);
        Assert.Equal(DocumentKind.Memo, document.Kind);
        Assert.Equal(SigningMode.Parallel, document.SigningMode);
        Assert.Equal("order.docx", document.File.FileName);
        Assert.False(document.File.IsPdf);
        Assert.Equal([World.SignerA2.Id], document.Signers.Select(static s => s.UserId));
        Assert.Contains(oldKey, World.Files.Deleted);
        Assert.Contains(AuditActions.DocumentUpdated, World.Audit.Actions());
    }

    [Fact]
    public async Task Changing_a_draft_without_a_file_keeps_the_file_and_can_send_it()
    {
        var id = await CreateAsync(submit: false);

        var result = await World.As(World.AuthorA).SendAsync(new UpdateDocumentCommand(
            id, "Наказ", null, DocumentKind.Order, World.World.AHr.Id, SigningMode.Sequential, [World.SignerA.Id], true, null));

        Assert.True(result.IsSuccess);
        var document = await GetAsync(World.AuthorA, id);
        Assert.Equal(DocumentStatus.InSigning, document.Status);
        Assert.Equal("order.pdf", document.File.FileName);
        Assert.Contains(AuditActions.DocumentSubmitted, World.Audit.Actions());
        Assert.Contains(World.NotificationEvents.Received, r => r.UserId == World.SignerA.Id && r.Notification.Kind == NotificationKind.DocumentToSign);
    }

    [Fact]
    public async Task Sending_a_changed_draft_needs_a_signer()
    {
        var id = await CreateAsync(submit: false);

        var result = await World.As(World.AuthorA).SendAsync(new UpdateDocumentCommand(
            id, "Наказ", null, DocumentKind.Order, World.World.AHr.Id, SigningMode.Sequential, [], true, null));

        Assert.Equal(ErrorType.Validation, result.Failure());
    }

    [Fact]
    public async Task A_document_in_signing_cannot_be_changed_and_a_new_file_is_removed_again()
    {
        var id = await CreateAsync(submit: true);

        var result = await World.As(World.AuthorA).SendAsync(new UpdateDocumentCommand(
            id, "Інша назва", null, DocumentKind.Order, World.World.AHr.Id, SigningMode.Sequential, [World.SignerA.Id], false, Upload("other.pdf", "%PDF other")));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Single(World.Files.In("documents"));
        Assert.Equal("Наказ про відпустки", (await GetAsync(World.AuthorA, id)).Title);
    }

    [Fact]
    public async Task Only_the_author_changes_a_document_and_others_do_not_even_see_it()
    {
        var id = await CreateAsync(submit: false);
        var update = new UpdateDocumentCommand(id, "Чужа правка", null, DocumentKind.Order, World.World.AHr.Id, SigningMode.Sequential, [World.SignerA.Id], false, null);

        Assert.Equal(ErrorType.Forbidden, (await World.As(World.RegistrarA).SendAsync(update)).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerB).SendAsync(update)).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.AuthorA).SendAsync(update with { Id = Guid.NewGuid() })).Failure());
    }

    [Fact]
    public async Task A_changed_document_must_stay_in_a_unit_of_the_author()
    {
        var id = await CreateAsync(submit: false);

        var result = await World.As(World.AuthorA).SendAsync(new UpdateDocumentCommand(
            id, "Наказ", null, DocumentKind.Order, World.World.BProj.Id, SigningMode.Sequential, [World.SignerA.Id], false, null));

        Assert.Equal(ErrorType.Validation, result.Failure());
    }

    [Fact]
    public async Task The_edit_form_is_given_only_to_the_author_of_an_editable_document()
    {
        var id = await CreateAsync(submit: false);

        var model = (await World.As(World.AuthorA).SendAsync(new GetDocumentForEdit.Query(id))).Ok();
        Assert.Equal([World.SignerA.Id, World.SignerA2.Id], model.Signers.Select(static s => s.Id));
        Assert.Equal(World.World.AHr.Id, model.Unit.Id);

        Assert.Equal(ErrorType.Forbidden, (await World.As(World.RegistrarA).SendAsync(new GetDocumentForEdit.Query(id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerB).SendAsync(new GetDocumentForEdit.Query(id))).Failure());

        await World.As(World.AuthorA).SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.InSigning));
        Assert.Equal(ErrorType.Conflict, (await World.As(World.AuthorA).SendAsync(new GetDocumentForEdit.Query(id))).Failure());
    }

    [Fact]
    public async Task The_author_sends_and_withdraws_a_document_until_someone_signs_it()
    {
        var id = await CreateAsync(submit: false);
        var author = World.As(World.AuthorA);

        Assert.True((await author.SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.InSigning))).IsSuccess);
        Assert.True((await GetAsync(World.AuthorA, id)).Actions.CanWithdraw);
        Assert.True((await author.SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.Draft))).IsSuccess);
        Assert.Equal(DocumentStatus.Draft, (await GetAsync(World.AuthorA, id)).Status);
        Assert.Contains(AuditActions.DocumentWithdrawn, World.Audit.Actions());

        await author.SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.InSigning));
        (await SignAsync(World.SignerA, id)).Ok();
        Assert.False((await GetAsync(World.AuthorA, id)).Actions.CanWithdraw);
        Assert.Equal(ErrorType.Validation, (await author.SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.Draft))).Failure());
    }

    [Fact]
    public async Task Only_the_author_sends_or_withdraws_and_a_state_cannot_be_set_directly()
    {
        var id = await CreateAsync(submit: false);

        Assert.Equal(ErrorType.Forbidden, (await World.As(World.RegistrarA).SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.InSigning))).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.AuthorA).SendAsync(new ChangeDocumentStatus.Command(Guid.NewGuid(), DocumentStatus.InSigning))).Failure());
        Assert.Equal(ErrorType.Validation, (await World.As(World.AuthorA).SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.Signed))).Failure());
        Assert.Equal(ErrorType.Validation, (await World.As(World.AuthorA).SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.Rejected))).Failure());
    }

    [Fact]
    public async Task A_completed_document_is_archived_by_its_author_or_a_document_manager()
    {
        var first = await CreateAsync(submit: true, signers: [World.SignerA]);
        var second = await CreateAsync(submit: true, signers: [World.SignerA]);
        (await SignAsync(World.SignerA, first)).Ok();
        (await SignAsync(World.SignerA, second)).Ok();

        Assert.Equal(ErrorType.Forbidden, (await World.As(World.SignerA).SendAsync(new ChangeDocumentStatus.Command(first, DocumentStatus.Archived))).Failure());
        Assert.True((await World.As(World.AuthorA).SendAsync(new ChangeDocumentStatus.Command(first, DocumentStatus.Archived))).IsSuccess);
        Assert.True((await World.As(World.RegistrarA).SendAsync(new ChangeDocumentStatus.Command(second, DocumentStatus.Archived))).IsSuccess);
        Assert.Equal(DocumentStatus.Archived, (await GetAsync(World.AuthorA, first)).Status);
        Assert.Contains(AuditActions.DocumentArchived, World.Audit.Actions());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerB).SendAsync(new ChangeDocumentStatus.Command(first, DocumentStatus.Archived))).Failure());
    }

    [Fact]
    public async Task A_document_in_signing_cannot_be_archived()
    {
        var id = await CreateAsync(submit: true);

        Assert.Equal(ErrorType.Validation, (await World.As(World.AuthorA).SendAsync(new ChangeDocumentStatus.Command(id, DocumentStatus.Archived))).Failure());
    }

    [Fact]
    public async Task A_document_without_signatures_is_deleted_with_its_file()
    {
        var id = await CreateAsync(submit: true);
        var key = World.Files.In("documents").Single();

        Assert.Equal(ErrorType.Forbidden, (await World.As(World.SignerA).SendAsync(new DeleteDocument.Command(id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerB).SendAsync(new DeleteDocument.Command(id))).Failure());
        Assert.True((await World.As(World.AuthorA).SendAsync(new DeleteDocument.Command(id))).IsSuccess);

        Assert.Empty(World.Db.Documents);
        Assert.Contains(key, World.Files.Deleted);
        Assert.Contains(AuditActions.DocumentDeleted, World.Audit.Actions());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.AuthorA).SendAsync(new DeleteDocument.Command(id))).Failure());
    }

    [Fact]
    public async Task A_document_manager_deletes_a_document_of_the_organization()
    {
        var id = await CreateAsync(submit: false);

        Assert.True((await World.As(World.RegistrarA).SendAsync(new DeleteDocument.Command(id))).IsSuccess);
    }

    [Fact]
    public async Task A_signed_document_cannot_be_deleted()
    {
        var id = await CreateAsync(submit: true, signers: [World.SignerA]);
        (await SignAsync(World.SignerA, id)).Ok();

        Assert.False((await GetAsync(World.AuthorA, id)).Actions.CanDelete);
        Assert.Equal(ErrorType.Validation, (await World.As(World.AuthorA).SendAsync(new DeleteDocument.Command(id))).Failure());
        Assert.NotEmpty(World.Db.Documents);
    }
}
