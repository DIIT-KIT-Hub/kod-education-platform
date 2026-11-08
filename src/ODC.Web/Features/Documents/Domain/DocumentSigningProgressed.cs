using ODC.Web.Shared.Kernel;

namespace ODC.Web.Features.Documents.Domain;

/// <summary>
/// The signing of a document moved on: it was sent for signing, signed by someone (maybe the last signer) or declined.
/// The state tells what happened (in signing: whose turn it is; signed; rejected).
/// </summary>
/// <param name="DocumentId">The document.</param>
public sealed record DocumentSigningProgressed(Guid DocumentId) : IDomainEvent;
