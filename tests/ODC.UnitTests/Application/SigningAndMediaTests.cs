using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.Media;
using ODC.Web.Features.Signing;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Files;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application;

/// <summary>
/// The journal of the file signing tool (only names and parameters reach the server) and the images of the news editor.
/// </summary>
public sealed class SigningAndMediaTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    /// <summary>
    /// The recorded audit events.
    /// </summary>
    private readonly RecordingAudit _audit = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task Signed_files_are_journaled_by_name_without_paths_with_the_parameters_and_the_certificate()
    {
        var result = await As(_world.Admin).SendAsync(new FileSigningRecord(
            ["C:\\Users\\me\\Desktop\\contract.pdf", " act.docx ", "  "],
            SignatureFormat.CAdES,
            " B-LT ",
            "Шевченко Тарас",
            "1234",
            "КНЕДП «Тест»"));

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditActions.FilesSigned, entry.Action);
        Assert.Equal("contract.pdf, act.docx: CAdES B-LT; Шевченко Тарас, КНЕДП «Тест», № 1234", entry.Details);
    }

    [Fact]
    public async Task A_journal_entry_without_a_level_or_certificate_has_the_format_only()
    {
        Assert.True((await As(_world.Admin).SendAsync(new FileSigningRecord(["a.txt"], SignatureFormat.XAdES, null, null, null, null))).IsSuccess);

        Assert.Equal("a.txt: XAdES", Assert.Single(_audit.Entries).Details);
    }

    [Fact]
    public async Task Overlong_names_are_cut_to_255_characters()
    {
        var name = new string('я', 300) + ".pdf";

        Assert.True((await As(_world.Admin).SendAsync(new FileSigningRecord([name], SignatureFormat.PAdES, null, null, null, null))).IsSuccess);

        Assert.StartsWith(new string('я', 255) + ": ", Assert.Single(_audit.Entries).Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_empty_or_too_long_list_of_files_is_refused()
    {
        var admin = As(_world.Admin);

        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new FileSigningRecord([" "], SignatureFormat.CAdES, null, null, null, null))).Failure());
        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new FileSigningRecord([.. Enumerable.Range(0, 51).Select(static i => $"{i}.pdf")], SignatureFormat.CAdES, null, null, null, null))).Failure());
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Only_users_of_the_signing_tool_journal_signed_files()
    {
        var result = await As(_world.EmployeeA).SendAsync(new FileSigningRecord(["a.pdf"], SignatureFormat.CAdES, null, null, null, null));

        Assert.Equal(ErrorType.Forbidden, result.Failure());
    }

    [Fact]
    public async Task A_news_editor_uploads_an_image_and_gets_its_addresses()
    {
        var media = (await As(_world.StewardA).SendAsync(new UploadNewsImage.Command(Image()))).Ok();

        Assert.Equal($"/media/{media.Id:N}", media.Url);
        Assert.Equal($"/media/{media.Id:N}/thumb", media.ThumbnailUrl);
        Assert.Contains(media.Id, _world.Storage.Stored);
        Assert.Contains(_world.Master.MediaFiles, m => m.Id == media.Id);
    }

    [Fact]
    public async Task A_file_the_image_processor_cannot_read_is_not_stored()
    {
        var result = await As(_world.StewardA).SendAsync(new UploadNewsImage.Command(new FileUpload(new MemoryStream([0, 2, 3]), "photo.png", "image/png", 3)));

        Assert.Equal(ErrorType.Validation, result.Failure());
        Assert.Empty(_world.Storage.Stored);
    }

    [Fact]
    public async Task Only_news_editors_upload_images()
    {
        Assert.Equal(ErrorType.Forbidden, (await As(_world.EmployeeA).SendAsync(new UploadNewsImage.Command(Image()))).Failure());
    }

    /// <summary>
    /// A small PNG file.
    /// </summary>
    /// <returns>The upload.</returns>
    private static FileUpload Image() => new(new MemoryStream([0x89, 0x50, 0x4E, 0x47, 1, 2, 3]), "photo.png", "image/png", 7);

    /// <summary>
    /// The services of a user with the recorded audit.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The services.</returns>
    private TwoOrganizations.Services As(User user) => _world.As(user, _audit.Register);
}
