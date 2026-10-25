using KOD.Application.Abstractions;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Help;

/// <summary>
/// Tells the reader whether the described feature is available to them and, if not, which permission it needs.
/// </summary>
public partial class HelpAccess
{
    /// <summary>
    /// Gets or sets the permissions of which at least one is needed; empty for everyone.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string> Permissions { get; set; } = [];

    /// <summary>
    /// Gets or sets the current user.
    /// </summary>
    [Inject]
    private ICurrentUser CurrentUser { get; set; } = default!;

    /// <summary>
    /// Gets a value indicating whether the current user has one of the permissions.
    /// </summary>
    private bool IsAvailable => Permissions.Any(CurrentUser.Has);

    /// <summary>
    /// Gets the Ukrainian names of the required permissions (the first two, then how many more).
    /// </summary>
    private string RequiredText
    {
        get
        {
            var names = Permissions.Select(static code => $"«{KOD.Domain.Access.Permissions.All.FirstOrDefault(p => p.Code == code)?.Title ?? code}»").ToList();
            return names.Count <= 2
                ? string.Join(" або ", names)
                : $"{string.Join(", ", names.Take(2))} або один з інших ({names.Count - 2})";
        }
    }

    /// <summary>
    /// Gets the full list of the required permissions for the tooltip.
    /// </summary>
    private string RequiredFullText => string.Join(
        ", ",
        Permissions.Select(static code => KOD.Domain.Access.Permissions.All.FirstOrDefault(p => p.Code == code)?.Title ?? code));
}
