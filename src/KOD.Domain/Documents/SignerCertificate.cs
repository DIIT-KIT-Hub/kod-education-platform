namespace KOD.Domain.Documents;

/// <summary>
/// Data of the qualified public key certificate that was used to create a signature.
/// </summary>
/// <param name="SubjectFullName">The signer's full name (certificate subject common name).</param>
/// <param name="SubjectOrganization">The organization from the certificate, if any.</param>
/// <param name="SubjectTaxId">The signer's taxpayer number (DRFO) or EDRPOU code, if present.</param>
/// <param name="IssuerName">The common name of the qualified trust service provider.</param>
/// <param name="SerialNumber">The certificate serial number (hex).</param>
/// <param name="ValidFrom">The start of the certificate validity period.</param>
/// <param name="ValidTo">The end of the certificate validity period.</param>
public sealed record SignerCertificate(
    string SubjectFullName,
    string? SubjectOrganization,
    string? SubjectTaxId,
    string IssuerName,
    string SerialNumber,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);
