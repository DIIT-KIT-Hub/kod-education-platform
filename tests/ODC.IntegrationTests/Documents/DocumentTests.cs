using System.Globalization;
using System.Net;
using System.Text;
using ODC.IntegrationTests.Clients;
using ODC.IntegrationTests.Hosting;
using ODC.Web.Endpoints;
using ODC.Web.Features.Documents;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Paging;

namespace ODC.IntegrationTests.Documents;

/// <summary>
/// Documents through the API: registration with a file and a signing route, who sees them, the file, the rejection, and
/// signers from another organization (which the move to a database per organization has to keep working).
/// </summary>
/// <param name="app">The application.</param>
[Collection(IntegrationCollection.Name)]
public sealed class DocumentTests(OdcApplication app)
{
    [Fact]
    public async Task A_submitted_document_waits_for_its_signer_and_stays_hidden_from_another_organization()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var director = await app.Accounts.SignInAsync(Demo.Director);
        using var outsider = await app.Accounts.SignInAsync(Demo.FundEmployee);
        const string text = "Наказ про відпустку";

        var id = await CreateAsync(engineer, "Наказ № 1", text, director.Context!.User!.Id);

        var awaiting = await director.GetAsync<PagedResult<DocumentListItem>>("/api/v1/documents?scope=AwaitingMe&pageSize=100");
        var item = Assert.Single(awaiting.Items, d => d.Id == id);
        Assert.True(item.AwaitingMe);
        Assert.Equal(DocumentStatus.InSigning, item.Status);

        using var file = await engineer.GetAsync($"/api/v1/documents/{id}/file");
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal(text, Encoding.UTF8.GetString(await file.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)));

        using var hidden = await outsider.GetAsync($"/api/v1/documents/{id}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using var hiddenFile = await outsider.GetAsync($"/api/v1/documents/{id}/file");
        Assert.Equal(HttpStatusCode.NotFound, hiddenFile.StatusCode);
        var outsiders = await outsider.GetAsync<PagedResult<DocumentListItem>>("/api/v1/documents?pageSize=100");
        Assert.DoesNotContain(outsiders.Items, d => d.Id == id);
    }

    [Fact]
    public async Task A_signer_rejects_a_document_with_a_reason()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var headOfIt = await app.Accounts.SignInAsync(Demo.HeadOfIt);
        var id = await CreateAsync(engineer, "Службова записка", "Прошу закупити ноутбук", headOfIt.Context!.User!.Id);

        await ApiClient.ExpectAsync(await headOfIt.PostAsync($"/api/v1/documents/{id}/rejections", new RejectionRequest("Немає бюджету")), HttpStatusCode.Created);

        var details = await engineer.GetAsync<DocumentDetails>($"/api/v1/documents/{id}");
        Assert.Equal(DocumentStatus.Rejected, details.Status);
        Assert.Contains(details.Signers, s => s.Comment == "Немає бюджету");
    }

    [Fact]
    public async Task A_signer_from_another_organization_sees_only_the_document_to_sign()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        using var fundHead = await app.Accounts.SignInAsync(Demo.FundHead);
        using var director = await app.Accounts.SignInAsync(Demo.Director);
        var id = await CreateAsync(engineer, "Договір про співпрацю", "Текст договору", fundHead.Context!.User!.Id);
        var unrelated = await CreateAsync(engineer, "Внутрішній наказ", "Лише для своїх", director.Context!.User!.Id);

        // The document lives in the engineer's organization: the head of the fund signs there as a guest.
        var details = await fundHead.GetAsync<DocumentDetails>($"/o/ORG/api/v1/documents/{id}");
        Assert.True(details.Actions.CanSign);
        using var other = await fundHead.GetAsync($"/o/ORG/api/v1/documents/{unrelated}");
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
    }

    [Fact]
    public async Task A_document_without_a_supported_file_type_is_refused()
    {
        using var engineer = await app.Accounts.SignInAsync(Demo.Engineer);
        var unit = (await engineer.GetAsync<List<UnitOption>>("/api/v1/documents/author-units"))[0];

        using var response = await engineer.SendFormAsync(
            HttpMethod.Post,
            "/api/v1/documents",
            [("Title", "Скрипт"), ("Kind", nameof(DocumentKind.Other)), ("OrganizationUnitId", unit.Id.ToString())],
            new UploadedFile("file", "run.exe", [0x4D, 0x5A], "application/octet-stream"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Registers a text document in the author's first unit and submits it to one signer.
    /// </summary>
    /// <param name="author">The author.</param>
    /// <param name="title">The title.</param>
    /// <param name="text">The file text.</param>
    /// <param name="signerId">The signer.</param>
    /// <returns>The document identifier.</returns>
    internal static async Task<Guid> CreateAsync(ApiClient author, string title, string text, Guid signerId)
    {
        var unit = (await author.GetAsync<List<UnitOption>>("/api/v1/documents/author-units"))[0];
        using var response = await author.SendFormAsync(
            HttpMethod.Post,
            "/api/v1/documents",
            [
                ("Title", title),
                ("Kind", nameof(DocumentKind.Memo)),
                ("OrganizationUnitId", unit.Id.ToString()),
                ("SigningMode", nameof(SigningMode.Parallel)),
                ("SignerIds", signerId.ToString()),
                ("Submit", bool.TrueString.ToLower(CultureInfo.InvariantCulture)),
            ],
            UploadedFile.Text("file", "document.txt", text));
        return (await ApiClient.ReadAsync<CreatedId>(response)).Id;
    }
}
