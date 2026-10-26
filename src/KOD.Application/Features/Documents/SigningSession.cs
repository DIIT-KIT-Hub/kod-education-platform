using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// What a signer needs to add the next signature: the version to add it to and how long the turn is held.
/// </summary>
/// <param name="BaseSignatureId">The newest signature to add the new one to (cumulative formats), or <see langword="null"/>.</param>
/// <param name="BaseSha256">The SHA-256 of that signature's file, checked by the browser before signing.</param>
/// <param name="Format">The format fixed by the first signature, or <see langword="null"/> when nobody signed yet.</param>
/// <param name="Packaging">The packaging fixed by the first signature.</param>
/// <param name="SignatureCount">The number of signatures the base file must already hold.</param>
/// <param name="TurnExpiresAt">The moment the signing turn ends.</param>
public sealed record SigningSession(
    Guid? BaseSignatureId,
    string? BaseSha256,
    SignatureFormat? Format,
    SignaturePackaging? Packaging,
    int SignatureCount,
    DateTimeOffset TurnExpiresAt);
