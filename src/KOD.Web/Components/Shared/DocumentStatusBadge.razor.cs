using KOD.Domain.Documents;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Shared;

/// <summary>
/// The status label of a document.
/// </summary>
public partial class DocumentStatusBadge
{
    /// <summary>
    /// Gets or sets the document state.
    /// </summary>
    [Parameter]
    public DocumentStatus Status { get; set; }
}
