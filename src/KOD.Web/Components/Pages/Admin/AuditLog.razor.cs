using KOD.Application.Common.Paging;
using KOD.Application.Features.Audit;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// The audit trail grid with category and period filters.
/// </summary>
public partial class AuditLog
{
    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Search { get; set; }

    /// <summary>
    /// Gets or sets the action prefix filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Action { get; set; }

    /// <summary>
    /// Gets or sets the start date.
    /// </summary>
    [SupplyParameterFromQuery]
    private DateOnly? From { get; set; }

    /// <summary>
    /// Gets or sets the end date.
    /// </summary>
    [SupplyParameterFromQuery]
    private DateOnly? To { get; set; }

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [SupplyParameterFromQuery]
    private int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [SupplyParameterFromQuery]
    private int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets the sort key.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether sorting is descending.
    /// </summary>
    [SupplyParameterFromQuery]
    private bool Descending { get; set; }

    /// <summary>
    /// Gets or sets the audit service.
    /// </summary>
    [Inject]
    private IAuditService Audit { get; set; } = default!;

    /// <summary>
    /// Gets or sets the loaded page.
    /// </summary>
    private PagedResult<AuditEntryDto>? Result { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var result = await Audit.ListAsync(
            new AuditQuery
            {
                Search = Search,
                Action = Action,
                From = From,
                To = To,
                Page = Page ?? 1,
                PageSize = PageSize ?? 50,
                SortBy = SortBy,
                Descending = Descending,
            },
            CancellationToken.None);
        Result = result.IsSuccess ? result.Value : PagedResult<AuditEntryDto>.Empty(new PageRequest());
    }
}
