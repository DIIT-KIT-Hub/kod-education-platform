using KOD.Domain.Common;
using KOD.Domain.Documents;

namespace KOD.UnitTests.Domain;

public sealed class DocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Author = Guid.CreateVersion7();
    private static readonly Guid First = Guid.CreateVersion7();
    private static readonly Guid Second = Guid.CreateVersion7();

    [Fact]
    public void Sequential_route_only_allows_the_current_signer()
    {
        var document = CreateDocument(SigningMode.Sequential);
        document.Submit(Now);

        Assert.True(document.IsAwaitingSignatureFrom(First));
        Assert.False(document.IsAwaitingSignatureFrom(Second));
        Assert.Throws<DomainException>(() => Sign(document, Second));

        Sign(document, First);
        Assert.True(document.IsAwaitingSignatureFrom(Second));
    }

    [Fact]
    public void Parallel_route_allows_any_pending_signer()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);

        Assert.True(document.IsAwaitingSignatureFrom(First));
        Assert.True(document.IsAwaitingSignatureFrom(Second));
    }

    [Fact]
    public void Document_becomes_signed_when_every_signer_signed()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);

        Sign(document, Second);
        Assert.Equal(DocumentStatus.InSigning, document.Status);

        Sign(document, First);
        Assert.Equal(DocumentStatus.Signed, document.Status);
        Assert.Equal(2, document.Signatures.Count);
        Assert.NotNull(document.CompletedAt);
    }

    [Fact]
    public void Rejection_stops_the_route_and_requires_a_reason()
    {
        var document = CreateDocument(SigningMode.Sequential);
        document.Submit(Now);

        Assert.Throws<DomainException>(() => document.Reject(First, "  ", Now));
        document.Reject(First, "Помилка в даті наказу", Now);

        Assert.Equal(DocumentStatus.Rejected, document.Status);
        Assert.False(document.IsAwaitingSignatureFrom(Second));
    }

    [Fact]
    public void Rejected_document_can_be_resubmitted_with_a_fresh_route()
    {
        var document = CreateDocument(SigningMode.Sequential);
        document.Submit(Now);
        document.Reject(First, "Виправити", Now);

        document.Submit(Now.AddHours(1));

        Assert.Equal(DocumentStatus.InSigning, document.Status);
        Assert.All(document.Signers, static s => Assert.Equal(SignerStatus.Pending, s.Status));
    }

    [Fact]
    public void Pades_is_rejected_for_non_pdf_files()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);

        var error = Assert.Throws<DomainException>(() => document.AddSignature(
            First, SignatureFormat.PAdES, SignaturePackaging.Enveloped, "B-LT", KeyMediaType.File, SignatureFile(), Certificate(), Now, Now));
        Assert.Contains("PDF", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Draft_without_signers_cannot_be_submitted()
    {
        var document = new Document("2026/000001", "Наказ", null, DocumentKind.Order, Author, Guid.CreateVersion7(), SigningMode.Sequential, DocumentFile());

        Assert.Throws<DomainException>(() => document.Submit(Now));
    }

    [Fact]
    public void Signed_document_cannot_be_deleted_or_withdrawn()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);
        Sign(document, First);

        Assert.Throws<DomainException>(document.EnsureCanBeDeleted);
        Assert.Throws<DomainException>(document.Withdraw);
    }

    private static Document CreateDocument(SigningMode mode)
    {
        var document = new Document("2026/000001", "Про затвердження графіка", null, DocumentKind.Order, Author, Guid.CreateVersion7(), mode, DocumentFile());
        document.SetRoute([First, Second, First], mode);
        return document;
    }

    private static void Sign(Document document, Guid signer)
        => document.AddSignature(signer, SignatureFormat.CAdES, SignaturePackaging.Detached, "B-LT", KeyMediaType.File, SignatureFile(), Certificate(), Now, Now);

    private static StoredFile DocumentFile() => new("order.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 1024, new string('a', 64), "documents/2026/10/" + new string('0', 32));

    private static StoredFile SignatureFile() => new("order.p7s", "application/pkcs7-signature", 2048, new string('b', 64), "signatures/2026/10/" + new string('1', 32));

    private static SignerCertificate Certificate() => new("Коваленко Марія Андріївна", null, "1234567890", "КНЕДП «Дія»", "5E984D526F82F38F", null, null);
}
