using KOD.Application.Common.Paging;
using KOD.Application.Features.Documents;
using KOD.Domain.Access;
using KOD.Domain.Documents;
using KOD.Web.Components.Shared;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages.Documents;

/// <summary>
/// The document registry with scope tabs, search, filters, sorting and pagination.
/// </summary>
public partial class DocumentsIndex
{
    /// <summary>
    /// Gets or sets the selected scope.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Status { get; set; }

    /// <summary>
    /// Gets or sets the kind filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Kind { get; set; }

    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Search { get; set; }

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
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the document service.
    /// </summary>
    [Inject]
    private IDocumentService Documents { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the loaded page.
    /// </summary>
    private PagedResult<DocumentListItem>? Result { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user can create documents.
    /// </summary>
    private bool CanCreate { get; set; }

    /// <summary>
    /// Gets the selected scope (defaults to the documents awaiting the user).
    /// </summary>
    private DocumentScope CurrentScope => QueryValues.Enum<DocumentScope>(Scope) ?? DocumentScope.AwaitingMe;

    /// <summary>
    /// Gets the empty state text of the current scope.
    /// </summary>
    private string EmptyText => CurrentScope == DocumentScope.AwaitingMe && Search is null
        ? "Документів, що очікують вашого підпису, немає."
        : "Спробуйте змінити параметри пошуку або фільтри.";

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        CanCreate = (await AuthenticationState).User.HasPermission(Permissions.Documents.Create);
        var result = await Documents.ListAsync(
            new DocumentQuery
            {
                Scope = CurrentScope,
                Status = QueryValues.Enum<DocumentStatus>(Status),
                Kind = QueryValues.Enum<DocumentKind>(Kind),
                Search = Search,
                Page = Page ?? 1,
                PageSize = PageSize ?? PageRequest.DefaultPageSize,
                SortBy = SortBy,
                Descending = Descending,
            },
            CancellationToken.None);

        Result = result.IsSuccess ? result.Value : PagedResult<DocumentListItem>.Empty(new PageRequest());
        ErrorMessage = result.IsSuccess ? null : result.Error.Message;
    }

    /// <summary>
    /// Builds the link of a scope tab (resets paging and filters).
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <returns>The URL.</returns>
    private string ScopeHref(DocumentScope scope) => Navigation.GetUriWithQueryParameters(
        "/documents",
        new Dictionary<string, object?> { ["scope"] = scope.ToString() });
}
