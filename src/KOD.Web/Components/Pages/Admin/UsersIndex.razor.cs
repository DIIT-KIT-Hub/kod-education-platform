using System.Security.Claims;
using KOD.Application.Common.Models;
using KOD.Application.Common.Paging;
using KOD.Application.Features.Organization;
using KOD.Application.Features.Roles;
using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// The user grid with filters, export and links to import and creation.
/// </summary>
public partial class UsersIndex
{
    /// <summary>
    /// Gets or sets the search term.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Search { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private string? Status { get; set; }

    /// <summary>
    /// Gets or sets the unit filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private Guid? UnitId { get; set; }

    /// <summary>
    /// Gets or sets the role filter.
    /// </summary>
    [SupplyParameterFromQuery]
    private Guid? RoleId { get; set; }

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
    /// Gets or sets the user service.
    /// </summary>
    [Inject]
    private IUserService Users { get; set; } = default!;

    /// <summary>
    /// Gets or sets the organization service.
    /// </summary>
    [Inject]
    private IOrganizationService Organization { get; set; } = default!;

    /// <summary>
    /// Gets or sets the role service.
    /// </summary>
    [Inject]
    private IRoleService RoleService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current user.
    /// </summary>
    private ClaimsPrincipal User { get; set; } = new();

    /// <summary>
    /// Gets or sets the loaded page.
    /// </summary>
    private PagedResult<UserListItem>? Result { get; set; }

    /// <summary>
    /// Gets or sets the unit options (the organizations whose accounts the current user sees).
    /// </summary>
    private IReadOnlyList<UnitOption> Units { get; set; } = [];

    /// <summary>
    /// Gets or sets the role options.
    /// </summary>
    private IReadOnlyList<RoleOption> Roles { get; set; } = [];

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        User = (await AuthenticationState).User;
        Units = await Organization.GetOptionsAsync(Permissions.Users.View, CancellationToken.None);
        Roles = await RoleService.GetOptionsAsync(CancellationToken.None);
        var result = await Users.ListAsync(BuildQuery(), CancellationToken.None);
        Result = result.IsSuccess ? result.Value : PagedResult<UserListItem>.Empty(new PageRequest());
    }

    /// <summary>
    /// Builds the query from the URL.
    /// </summary>
    /// <returns>The query.</returns>
    private UserQuery BuildQuery() => new()
    {
        Search = Search,
        Status = QueryValues.Enum<UserStatusFilter>(Status) ?? UserStatusFilter.All,
        UnitId = UnitId,
        RoleId = RoleId,
        Page = Page ?? 1,
        PageSize = PageSize ?? PageRequest.DefaultPageSize,
        SortBy = SortBy,
        Descending = Descending,
    };

    /// <summary>
    /// Builds the export link with the current filters.
    /// </summary>
    /// <param name="format">The file format.</param>
    /// <returns>The URL.</returns>
    private string ExportHref(string format) => Navigation.GetUriWithQueryParameters(
        "/api/v1/user-exports",
        new Dictionary<string, object?>
        {
            ["format"] = format,
            ["search"] = Search,
            ["status"] = Status,
            ["unitId"] = UnitId,
            ["roleId"] = RoleId,
        });
}
