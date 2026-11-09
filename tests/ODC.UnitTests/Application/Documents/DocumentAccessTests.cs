using ODC.UnitTests.Infrastructure;
using ODC.Web.Features.Documents;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.Home;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application.Documents;

/// <summary>
/// Who sees which document (OWASP API1: object-level access), the register's sections, filters and paging, the file
/// downloads and the editor's look-ups.
/// </summary>
/// <param name="database">The PostgreSQL test database of the class.</param>
[Collection(PostgresCollection.Name)]
public sealed class DocumentAccessTests(PostgresDatabase database) : DocumentTestBase(database)
{
    [Fact]
    public async Task The_author_and_the_signers_see_a_document_and_people_without_a_role_in_it_do_not()
    {
        var id = await CreateAsync(submit: true);

        Assert.True((await World.As(World.AuthorA).SendAsync(new GetDocument.Query(id))).IsSuccess);
        Assert.True((await World.As(World.SignerA2).SendAsync(new GetDocument.Query(id))).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await World.As(World.ReaderA).SendAsync(new GetDocument.Query(id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerB).SendAsync(new GetDocument.Query(id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.OutsiderA).SendAsync(new GetDocument.Query(id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await World.As(World.AuthorA).SendAsync(new GetDocument.Query(Guid.NewGuid()))).Failure());
    }

    [Fact]
    public async Task Wider_viewing_permissions_open_the_documents_of_a_unit_an_organization_or_the_whole_cabinet()
    {
        var id = await CreateAsync(submit: false);

        Assert.True((await World.As(World.UnitViewerHr).SendAsync(new GetDocument.Query(id))).IsSuccess);
        Assert.True((await World.As(World.RegistrarA).SendAsync(new GetDocument.Query(id))).IsSuccess);
        Assert.True((await World.As(World.AuditorB).SendAsync(new GetDocument.Query(id))).IsSuccess);
    }

    [Fact]
    public async Task A_viewer_sees_what_others_may_do_but_may_not_do_it()
    {
        var id = await CreateAsync(submit: true);

        var actions = (await GetAsync(World.AuditorB, id)).Actions;

        Assert.False(actions.CanEdit);
        Assert.False(actions.CanSign);
        Assert.False(actions.CanWithdraw);
        Assert.False(actions.CanDelete);
        Assert.False(actions.CanArchive);
    }

    [Fact]
    public async Task A_document_manager_may_delete_but_not_edit_a_document_of_another_author()
    {
        var id = await CreateAsync(submit: false);

        var actions = (await GetAsync(World.RegistrarA, id)).Actions;

        Assert.True(actions.CanDelete);
        Assert.False(actions.CanEdit);
        Assert.False(actions.CanSubmit);
    }

    [Fact]
    public async Task The_document_shows_the_route_with_positions_and_the_signatures()
    {
        var id = await CreateAsync(submit: true);
        (await SignAsync(World.SignerA, id)).Ok();

        var document = await GetAsync(World.AuthorA, id);

        Assert.Equal("Автор Анна", document.Author.FullName);
        Assert.Equal(World.World.AHr.Id, document.Unit.Id);
        Assert.Equal([1, 2], document.Signers.Select(static s => s.Order));
        Assert.NotNull(document.Signers[0].Position);
        Assert.Equal(SignerStatus.Signed, document.Signers[0].Status);
        var signature = Assert.Single(document.Signatures);
        Assert.Equal(World.SignerA.Id, signature.UserId);
        Assert.Equal(SignatureFormat.CAdES, signature.Format);
        Assert.Equal(document.Signatures[0].Id, document.CombinedSignatureId);
    }

    [Fact]
    public async Task Each_section_of_the_register_lists_the_right_documents()
    {
        var awaiting = await CreateAsync(submit: true, signers: [World.SignerA]);
        var signed = await CreateAsync(submit: true, signers: [World.SignerA]);
        var later = await CreateAsync(submit: true, signers: [World.SignerA2, World.SignerA]);
        var draft = await CreateAsync(submit: false, signers: [World.SignerA]);
        (await SignAsync(World.SignerA, signed)).Ok();

        var signer = World.As(World.SignerA);
        Assert.Equal([awaiting], await IdsAsync(signer, new DocumentQuery { Scope = DocumentScope.AwaitingMe }));
        Assert.Equal([signed], await IdsAsync(signer, new DocumentQuery { Scope = DocumentScope.SignedByMe }));
        Assert.Equal(new[] { awaiting, signed, later, draft }.Order(), (await IdsAsync(signer, new DocumentQuery { Scope = DocumentScope.Involved })).Order());
        Assert.Equal(new[] { awaiting, signed, later, draft }.Order(), (await IdsAsync(World.As(World.AuthorA), new DocumentQuery { Scope = DocumentScope.Authored })).Order());
        Assert.Empty(await IdsAsync(World.As(World.ReaderA), new DocumentQuery { Scope = DocumentScope.Accessible }));
        Assert.Equal(4, (await IdsAsync(World.As(World.AuditorB), new DocumentQuery())).Count);

        var item = (await signer.SendAsync(new DocumentQuery { Scope = DocumentScope.AwaitingMe })).Ok().Items.Single();
        Assert.True(item.AwaitingMe);
        Assert.Equal("Автор Анна", item.AuthorName);
        Assert.Equal(0, item.SignedCount);
        Assert.Equal(1, item.SignersCount);
    }

    [Fact]
    public async Task The_register_filters_by_state_kind_and_text_and_pages_the_result()
    {
        var first = (await World.As(World.AuthorA).SendAsync(Create([World.SignerA], title: "Положення про архів"))).Ok();
        var second = (await World.As(World.AuthorA).SendAsync(Create([World.SignerA], submit: true, title: "Наказ про премію"))).Ok();
        await World.As(World.AuthorA).SendAsync(new CreateDocumentCommand("Службова записка", null, DocumentKind.Memo, World.World.AHr.Id, SigningMode.Sequential, [World.SignerA.Id], false, Upload()));
        var author = World.As(World.AuthorA);

        Assert.Equal([second], await IdsAsync(author, new DocumentQuery { Scope = DocumentScope.Authored, Status = DocumentStatus.InSigning }));
        Assert.Equal(2, (await IdsAsync(author, new DocumentQuery { Scope = DocumentScope.Authored, Kind = DocumentKind.Order })).Count);
        Assert.Equal([first], await IdsAsync(author, new DocumentQuery { Scope = DocumentScope.Authored, Search = "архів" }));
        Assert.Equal([second], await IdsAsync(author, new DocumentQuery { Scope = DocumentScope.Authored, Search = "000002" }));

        var page = (await author.SendAsync(new DocumentQuery { Scope = DocumentScope.Authored, Page = 2, PageSize = 2, SortBy = "title" })).Ok();
        Assert.Equal(3, page.TotalCount);
        Assert.Equal("Службова записка", Assert.Single(page.Items).Title);

        var descending = (await author.SendAsync(new DocumentQuery { Scope = DocumentScope.Authored, SortBy = "number", Descending = true })).Ok();
        Assert.Equal(["2026/000003", "2026/000002", "2026/000001"], descending.Items.Select(static d => d.RegistrationNumber));
    }

    [Fact]
    public async Task Listing_needs_the_viewing_permission()
    {
        var result = await World.As(World.OutsiderA).SendAsync(new DocumentQuery());

        Assert.Equal(ErrorType.Forbidden, result.Failure());
    }

    [Fact]
    public async Task The_file_is_downloaded_only_by_those_who_see_the_document()
    {
        var id = await CreateAsync(submit: false);

        var file = (await World.As(World.SignerA).SendAsync(new DownloadDocumentFiles.FileQuery(id))).Ok();
        using var reader = new StreamReader(file.Content);
        Assert.Equal("%PDF-1.4 document", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
        Assert.Equal("order.pdf", file.FileName);
        Assert.Equal("application/pdf", file.ContentType);

        Assert.Equal(ErrorType.NotFound, (await World.As(World.SignerB).SendAsync(new DownloadDocumentFiles.FileQuery(id))).Failure());
    }

    [Fact]
    public async Task An_author_registers_documents_in_the_units_of_their_positions_and_a_manager_in_the_organization()
    {
        var clerk = await World.As(World.AuthorA).SendAsync(new GetDocumentForEdit.AuthorUnitsQuery());
        var registrar = await World.As(World.RegistrarA).SendAsync(new GetDocumentForEdit.AuthorUnitsQuery());

        Assert.Contains(clerk, u => u.Id == World.World.AHr.Id);
        Assert.DoesNotContain(clerk, u => u.Id == World.World.BProj.Id);
        Assert.Contains(registrar, u => u.Id == World.World.A.Id);
        Assert.Contains(registrar, u => u.Id == World.World.AHr.Id);
        Assert.DoesNotContain(registrar, u => u.Id == World.World.B.Id);
    }

    [Fact]
    public async Task Signers_are_searched_from_two_letters_among_active_users()
    {
        var author = World.As(World.AuthorA);

        Assert.Empty(await author.SendAsync(new GetDocumentForEdit.SignersQuery("Б")));
        Assert.Contains(await author.SendAsync(new GetDocumentForEdit.SignersQuery("Бонд")), u => u.Id == World.SignerA.Id);
        Assert.Contains(await author.SendAsync(new GetDocumentForEdit.SignersQuery("signer.a2@")), u => u.Id == World.SignerA2.Id);

        World.SignerA.SetActive(false);
        World.Db.SaveChanges();
        Assert.DoesNotContain(await author.SendAsync(new GetDocumentForEdit.SignersQuery("Бонд")), u => u.Id == World.SignerA.Id);
        Assert.Empty(await World.As(World.ReaderA).SendAsync(new GetDocumentForEdit.SignersQuery("Бонд")));
    }

    [Fact]
    public async Task The_home_page_counts_the_documents_waiting_for_the_user_and_their_own()
    {
        await CreateAsync(submit: true, signers: [World.SignerA]);
        var signed = await CreateAsync(submit: true, signers: [World.SignerA]);
        await CreateAsync(submit: false, signers: [World.SignerA]);
        (await SignAsync(World.SignerA, signed)).Ok();

        var signer = await World.As(World.SignerA).SendAsync(new GetDashboard.Query());
        var author = await World.As(World.AuthorA).SendAsync(new GetDashboard.Query());

        Assert.Equal(1, signer.AwaitingMyDecision);
        Assert.Single(signer.Awaiting);
        Assert.Equal(1, signer.SignedThisMonth);
        Assert.Equal(1, author.MyDrafts);
        Assert.Equal(1, author.MyInSigning);
    }

    /// <summary>
    /// Lists the identifiers of a register page.
    /// </summary>
    /// <param name="session">The user's services.</param>
    /// <param name="query">The section, filters and paging.</param>
    /// <returns>The identifiers.</returns>
    private static async Task<List<Guid>> IdsAsync(DocumentWorld.Session session, DocumentQuery query)
        => [.. (await session.SendAsync(query)).Ok().Items.Select(static d => d.Id)];
}
