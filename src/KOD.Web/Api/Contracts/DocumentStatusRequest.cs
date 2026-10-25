using KOD.Domain.Documents;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Body of <c>PUT /api/v1/documents/{id}/status</c>: the target life-cycle state.
/// Allowed transitions: Draft/Rejected → InSigning (submit), InSigning → Draft (withdraw), Signed/Rejected → Archived.
/// </summary>
/// <param name="Status">The target state.</param>
public sealed record DocumentStatusRequest(DocumentStatus Status);
