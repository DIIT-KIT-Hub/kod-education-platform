using System.Text;
using ODC.UnitTests.Infrastructure;
using ODC.Web.Features.Documents;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Shared.Domain.Users;
using ODC.Web.Shared.Files;

namespace ODC.UnitTests.Application.Documents;

/// <summary>
/// The common steps of the document tests: a fresh <see cref="DocumentWorld"/> per test and helpers that create, submit
/// and sign documents through the real pipeline.
/// </summary>
public abstract class DocumentTestBase : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentTestBase"/> class on an emptied PostgreSQL test database
    /// (documents keep their file as a complex property, which the in-memory provider cannot load).
    /// </summary>
    /// <param name="database">The test database of the class.</param>
    internal DocumentTestBase(PostgresDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        database.Reset();
        World = new DocumentWorld(database.Options);
    }

    /// <summary>
    /// Gets the cabinet of the test.
    /// </summary>
    internal DocumentWorld World { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        World.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Builds an uploaded file.
    /// </summary>
    /// <param name="name">The file name.</param>
    /// <param name="text">The content.</param>
    /// <param name="length">The reported length (the real one when null).</param>
    /// <returns>The upload.</returns>
    internal static FileUpload Upload(string name = "order.pdf", string text = "%PDF-1.4 document", long? length = null)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new FileUpload(new MemoryStream(bytes), name, "application/octet-stream", length ?? bytes.Length);
    }

    /// <summary>
    /// Builds the command that creates a document.
    /// </summary>
    /// <param name="signers">The signers in route order.</param>
    /// <param name="submit">Whether to send it for signing at once.</param>
    /// <param name="mode">The signing mode.</param>
    /// <param name="file">The file (a PDF by default).</param>
    /// <param name="unit">The unit (the HR division of A by default).</param>
    /// <param name="title">The title.</param>
    /// <returns>The command.</returns>
    internal CreateDocumentCommand Create(
        IReadOnlyList<User> signers,
        bool submit = false,
        SigningMode mode = SigningMode.Sequential,
        FileUpload? file = null,
        Guid? unit = null,
        string title = "Наказ про відпустки")
        => new(title, "Опис", DocumentKind.Order, unit ?? World.World.AHr.Id, mode, [.. signers.Select(static s => s.Id)], submit, file ?? Upload());

    /// <summary>
    /// Creates a document as the author of A and returns it.
    /// </summary>
    /// <param name="submit">Whether to send it for signing at once.</param>
    /// <param name="mode">The signing mode.</param>
    /// <param name="signers">The signers (SignerA, then SignerA2, by default).</param>
    /// <returns>The identifier.</returns>
    internal async Task<Guid> CreateAsync(bool submit = true, SigningMode mode = SigningMode.Sequential, params User[] signers)
    {
        var route = signers.Length > 0 ? signers : [World.SignerA, World.SignerA2];
        return (await World.As(World.AuthorA).SendAsync(Create(route, submit, mode))).Ok();
    }

    /// <summary>
    /// Signs a document as a signer (a CAdES signature by default). A cumulative signature is added to the newest one.
    /// </summary>
    /// <param name="signer">The signer.</param>
    /// <param name="documentId">The document.</param>
    /// <param name="format">The format.</param>
    /// <param name="serials">The serial numbers in the signature file (the signer's own last); the signer's e-mail when empty.</param>
    /// <param name="baseSignatureId">The signature the new one is added to (the newest one when null and cumulative).</param>
    /// <returns>The result.</returns>
    internal async Task<ODC.Web.Shared.Kernel.Result<Guid>> SignAsync(
        User signer,
        Guid documentId,
        SignatureFormat format = SignatureFormat.CAdES,
        string[]? serials = null,
        Guid? baseSignatureId = null)
    {
        var details = (await World.As(signer).SendAsync(new GetDocument.Query(documentId))).Ok();
        var latest = baseSignatureId ?? details.CombinedSignatureId;
        var previous = details.Signatures.Select(static s => s.Certificate.SerialNumber).ToArray();
        var content = serials ?? (SignatureChain.IsCumulative(format) && latest is not null ? [.. previous, signer.Email!] : [signer.Email!]);
        var bytes = DocumentWorld.ScriptedSignatureInspector.File(content);
        return await World.As(signer).SendAsync(new AddSignatureCommand(
            documentId,
            format,
            format == SignatureFormat.PAdES ? SignaturePackaging.Enveloped : SignaturePackaging.Detached,
            "B-LT",
            KeyMediaType.File,
            null,
            DocumentWorld.ScriptedSignatureInspector.Certificate(signer.Email!),
            new FileUpload(new MemoryStream(bytes), "signature.p7s", "application/pkcs7-signature", bytes.Length),
            SignatureChain.IsCumulative(format) ? latest : null));
    }

    /// <summary>
    /// Reads a document as a user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="documentId">The document.</param>
    /// <returns>The details.</returns>
    internal async Task<DocumentDetails> GetAsync(User user, Guid documentId)
        => (await World.As(user).SendAsync(new GetDocument.Query(documentId))).Ok();
}
