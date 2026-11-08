using ODC.Web.Features.Documents.Domain;
using ODC.Web.Messaging;
using ODC.Web.Security;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Paging;

namespace ODC.Web.Features.Documents;

/// <summary>
/// Paging and filter parameters of the document registry.
/// </summary>
[RequirePermission(Permissions.Documents.View)]
public sealed record DocumentQuery : PageRequest, IQuery<Result<PagedResult<DocumentListItem>>>
{
    /// <summary>
    /// Gets the list scope (every visible document when not given).
    /// </summary>
    public DocumentScope? Scope { get; init; }

    /// <summary>
    /// Gets the status filter.
    /// </summary>
    public DocumentStatus? Status { get; init; }

    /// <summary>
    /// Gets the kind filter.
    /// </summary>
    public DocumentKind? Kind { get; init; }
}
