using KOD.Domain.Common;
using KOD.Domain.Documents;

namespace KOD.UnitTests.Domain;

public sealed class DocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Author = Guid.CreateVersion7();
    private static readonly Guid First = Guid.CreateVersion7();
    private static readonly Guid Second = Guid.CreateVersion7();
    private static readonly Guid Third = Guid.CreateVersion7();

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
    public void Rejected_document_without_signatures_restarts_the_whole_route()
    {
        var document = CreateDocument(SigningMode.Sequential);
        document.Submit(Now);
        document.Reject(First, "Виправити", Now);

        document.Submit(Now.AddHours(1));

        Assert.Equal(DocumentStatus.InSigning, document.Status);
        Assert.All(document.Signers, static s => Assert.Equal(SignerStatus.Pending, s.Status));
    }

    [Fact]
    public void Resubmission_keeps_signatures_on_the_unchanged_file()
    {
        var document = CreateDocument(SigningMode.Sequential);
        document.Submit(Now);
        Sign(document, First);
        document.Reject(Second, "Додати підпис директора", Now);

        document.Submit(Now.AddHours(1));

        Assert.Equal(DocumentStatus.InSigning, document.Status);
        Assert.Equal(SignerStatus.Signed, document.Signers.Single(s => s.UserId == First).Status);
        Assert.False(document.IsAwaitingSignatureFrom(First));
        Assert.True(document.IsAwaitingSignatureFrom(Second));
        Assert.Single(document.Signatures);
    }

    [Fact]
    public void Replacing_the_file_removes_every_signature()
    {
        var document = CreateDocument(SigningMode.Sequential);
        document.Submit(Now);
        Sign(document, First);
        document.Reject(Second, "Неправильний файл", Now);

        var removed = document.ReplaceFile(DocumentFile());
        document.Submit(Now.AddHours(1));

        Assert.Single(removed);
        Assert.Empty(document.Signatures);
        Assert.All(document.Signers, static s => Assert.Equal(SignerStatus.Pending, s.Status));
        Assert.True(document.IsAwaitingSignatureFrom(First));
    }

    [Fact]
    public void Route_changes_remove_only_signatures_of_removed_signers()
    {
        var third = Guid.CreateVersion7();
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);
        Sign(document, First);
        document.Reject(Second, "Потрібен ще один підписант", Now);

        Assert.Empty(document.SetRoute([First, Second, third], SigningMode.Parallel));
        Assert.Single(document.Signatures);

        var removed = document.SetRoute([Second, third], SigningMode.Parallel);
        Assert.Single(removed);
        Assert.Empty(document.Signatures);
    }

    [Fact]
    public void Resubmitting_when_everyone_already_signed_completes_the_document()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);
        Sign(document, First);
        document.Reject(Second, "Підписант не потрібен", Now);

        document.SetRoute([First], SigningMode.Parallel);
        document.Submit(Now.AddHours(1));

        Assert.Equal(DocumentStatus.Signed, document.Status);
        Assert.NotNull(document.CompletedAt);
    }

    [Fact]
    public void Only_drafts_and_rejected_documents_can_be_edited()
    {
        var document = CreateDocument(SigningMode.Sequential);
        document.UpdateDetails("Нова назва", "Опис", DocumentKind.Memo, Guid.CreateVersion7());
        Assert.Equal("Нова назва", document.Title);

        document.Submit(Now);
        Assert.False(document.IsEditable);
        Assert.Throws<DomainException>(() => document.UpdateDetails("Інша", null, DocumentKind.Order, document.OrganizationUnitId));
        Assert.Throws<DomainException>(() => document.ReplaceFile(DocumentFile()));
        Assert.Throws<DomainException>(() => document.SetRoute([First], SigningMode.Sequential));
    }

    [Fact]
    public void Pades_is_rejected_for_non_pdf_files()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);

        var error = Assert.Throws<DomainException>(() => document.AddSignature(
            First, SignatureFormat.PAdES, SignaturePackaging.Enveloped, "B-LT", KeyMediaType.File, SignatureFile(), Certificate(), Now, Now, null));
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

    [Fact]
    public void Cumulative_signature_is_added_to_the_newest_version()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);
        var first = Sign(document, First);

        // A signature made on the original instead of the signed version would drop the first signature.
        Assert.Throws<DomainException>(() => document.AddSignature(
            Second, SignatureFormat.CAdES, SignaturePackaging.Detached, "B-LT", KeyMediaType.File, SignatureFile(), Certificate(), Now, Now.AddMinutes(1), null));

        var second = Sign(document, Second);
        Assert.Equal(first.Id, second.BaseSignatureId);
        Assert.Same(second, document.LatestSignature);
        Assert.Equal(DocumentStatus.Signed, document.Status);
    }

    [Fact]
    public void Later_signers_keep_the_format_of_the_first_signature()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);
        Sign(document, First);

        Assert.Throws<DomainException>(() => Sign(document, Second, SignatureFormat.XAdES));
    }

    [Fact]
    public void Independent_xades_signatures_have_no_base()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);

        Sign(document, First, SignatureFormat.XAdES);
        var second = Sign(document, Second, SignatureFormat.XAdES);

        Assert.Null(second.BaseSignatureId);
    }

    [Fact]
    public void Signing_turn_keeps_parallel_signers_in_order()
    {
        var document = CreateDocument(SigningMode.Parallel);
        document.Submit(Now);

        var expiresAt = document.BeginSigning(First, Now);
        Assert.Equal(Now + SignatureChain.TurnDuration, expiresAt);
        Assert.True(document.IsSigningTurnHeldByOther(Second, Now));
        Assert.Throws<DomainException>(() => document.BeginSigning(Second, Now));
        Assert.Throws<DomainException>(() => Sign(document, Second));

        // Signing ends the turn; an abandoned turn simply expires.
        Sign(document, First);
        Assert.False(document.IsSigningTurnHeldByOther(Second, Now));
        document.BeginSigning(Second, Now);

        var other = CreateDocument(SigningMode.Parallel);
        other.Submit(Now);
        other.BeginSigning(First, Now);
        Assert.False(other.IsSigningTurnHeldByOther(Second, expiresAt.AddSeconds(1)));
    }

    [Fact]
    public void Removing_a_signer_restarts_a_cumulative_chain()
    {
        var document = new Document("2026/000001", "Про затвердження графіка", null, DocumentKind.Order, Author, Guid.CreateVersion7(), SigningMode.Parallel, DocumentFile());
        document.SetRoute([First, Second, Third], SigningMode.Parallel);
        document.Submit(Now);
        Sign(document, First);
        Sign(document, Second);
        document.Reject(Third, "Потрібні правки", Now);

        // The first signature lives inside the second signer's file too, so removing the second signer drops both.
        var removed = document.SetRoute([First, Third], SigningMode.Parallel);

        Assert.Equal(2, removed.Count);
        Assert.Empty(document.Signatures);
    }

    private static Document CreateDocument(SigningMode mode)
    {
        var document = new Document("2026/000001", "Про затвердження графіка", null, DocumentKind.Order, Author, Guid.CreateVersion7(), mode, DocumentFile());
        document.SetRoute([First, Second, First], mode);
        return document;
    }

    private static DocumentSignature Sign(Document document, Guid signer, SignatureFormat format = SignatureFormat.CAdES)
        => document.AddSignature(signer, format, SignaturePackaging.Detached, "B-LT", KeyMediaType.File, SignatureFile(), Certificate(), Now, Now.AddSeconds(document.Signatures.Count), SignatureChain.IsCumulative(format) ? document.LatestSignature?.Id : null);

    private static StoredFile DocumentFile() => new("order.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 1024, new string('a', 64), "documents/2026/10/" + new string('0', 32));

    private static StoredFile SignatureFile() => new("order.p7s", "application/pkcs7-signature", 2048, new string('b', 64), "signatures/2026/10/" + new string('1', 32));

    private static SignerCertificate Certificate() => new("Коваленко Марія Андріївна", null, "1234567890", "КНЕДП «Дія»", "5E984D526F82F38F", null, null);
}
