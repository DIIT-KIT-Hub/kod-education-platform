using System.Text;
using KOD.Web.Signing;

namespace KOD.UnitTests.Web;

public sealed class TrustListTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Bundled_trust_list_is_valid_and_lists_provider_servers()
    {
        var valid = TrustList.TryCreate(Bundled("CAs.json"), Bundled("CACertificates.p7b"), Now, out var list, out var error);

        Assert.True(valid, error);
        Assert.True(list!.ProviderCount >= 30);
        Assert.True(list.CertificateCount >= 500);
        Assert.True(list.ContainsHost("acsk.privatbank.ua"));
        Assert.True(list.ContainsHost("ca.diia.gov.ua"));
        Assert.True(list.ContainsHost("CA.MONOBANK.UA"));
        Assert.NotEqual(list.ProvidersETag, list.CertificatesETag);
    }

    [Fact]
    public void Truncated_or_substituted_files_are_rejected()
    {
        var certificates = Bundled("CACertificates.p7b");

        Assert.False(TrustList.TryCreate(Encoding.UTF8.GetBytes("<html>error</html>"), certificates, Now, out _, out _));
        Assert.False(TrustList.TryCreate(Encoding.UTF8.GetBytes("""[{"issuerCNs":["A"]}]"""), certificates, Now, out _, out var tooFew));
        Assert.Contains("Too few", tooFew, StringComparison.Ordinal);
        Assert.False(TrustList.TryCreate(Bundled("CAs.json"), certificates[..100], Now, out _, out _));
    }

    [Theory]
    [InlineData("ca.edin.ua/services/ocsp/", "ca.edin.ua")]
    [InlineData("http://acsk.privatbank.ua/services/tsp/", "acsk.privatbank.ua")]
    [InlineData("CA.Tax.Gov.UA:80", "ca.tax.gov.ua")]
    [InlineData("", null)]
    [InlineData("bad host/x", null)]
    public void Hosts_are_extracted_from_addresses(string address, string? expected)
        => Assert.Equal(expected, TrustList.HostOf(address));

    private static byte[] Bundled(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TrustList", name));
}
