using System.Text;
using KOD.Domain.Documents;
using KOD.Infrastructure.Signatures;

namespace KOD.UnitTests.Infrastructure;

public sealed class SignatureInspectorTests
{
    [Fact]
    public void Counts_the_signature_dictionaries_of_a_pdf()
    {
        var inspector = new CmsSignatureInspector();
        var unsigned = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj << /Type /Catalog >> endobj\n%%EOF\n");
        var twice = Encoding.ASCII.GetBytes("%PDF-1.7\n<< /Type /Sig /ByteRange [0 10 20 30] >>\n%%EOF\n<< /Type /Sig /ByteRange [0 40 50 60] >>\n%%EOF\n");

        Assert.Equal(0, inspector.TryCountSignatures(unsigned, SignatureFormat.PAdES));
        Assert.Equal(2, inspector.TryCountSignatures(twice, SignatureFormat.PAdES));
    }

    [Fact]
    public void Unparsable_containers_are_not_counted()
    {
        var inspector = new CmsSignatureInspector();
        var garbage = Encoding.ASCII.GetBytes("not a signature");

        Assert.Null(inspector.TryCountSignatures(garbage, SignatureFormat.CAdES));
        Assert.Null(inspector.TryCountSignatures(garbage, SignatureFormat.AsicE));
        Assert.Null(inspector.TryReadSigners(garbage, SignatureFormat.CAdES));
    }
}
