using KOD.Application.Common.Paging;
using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// Paging and filter parameters of the document registry.
/// </summary>
public sealed record DocumentQuery : PageRequest
{
    /// <summary>
    /// Gets the list scope.
    /// </summary>
    public DocumentScope Scope { get; init; }

    /// <summary>
    /// Gets the status filter.
    /// </summary>
    public DocumentStatus? Status { get; init; }

    /// <summary>
    /// Gets the kind filter.
    /// </summary>
    public DocumentKind? Kind { get; init; }
}
