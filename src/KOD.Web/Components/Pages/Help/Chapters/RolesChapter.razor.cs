using KOD.Application.Abstractions;
using KOD.Domain.Access;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Help.Chapters;

/// <summary>
/// Manual chapter: every permission and the predefined roles. Rendered from the domain catalogs
/// (<see cref="Permissions.All"/>, <see cref="SystemRoles.All"/>), so it always matches the running version.
/// </summary>
public partial class RolesChapter
{
    /// <summary>
    /// Gets the permissions grouped by module.
    /// </summary>
    private static IEnumerable<IGrouping<AppModule, PermissionDefinition>> ModuleGroups => Permissions.All.GroupBy(static p => p.Module);

    /// <summary>
    /// Gets the name of the administrator role.
    /// </summary>
    private static string AdministratorName => SystemRoles.All.First(static r => r.Code == SystemRoles.Administrator).Name;

    /// <summary>
    /// Gets or sets the current user (marks the reader's own permissions).
    /// </summary>
    [Inject]
    private ICurrentUser CurrentUser { get; set; } = default!;

    /// <summary>
    /// Gets the Ukrainian title of a permission.
    /// </summary>
    /// <param name="code">The permission code.</param>
    /// <returns>The title.</returns>
    private static string TitleOf(string code) => Permissions.All.FirstOrDefault(p => p.Code == code)?.Title ?? code;
}
