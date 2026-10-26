using KOD.Application.Abstractions;
using KOD.Domain.Localization;
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
    /// Gets the names of the required permissions in the interface language (the first two, then how many more).
    /// </summary>
    private string RequiredText
    {
        get
        {
            var names = Permissions.Select(static code => Texts.T("«{0}»", TitleOf(code))).ToList();
            return names.Count switch
            {
                1 => names[0],
                2 => Texts.T("{0} або {1}", names[0], names[1]),
                _ => Texts.T("{0}, {1} або один з інших ({2})", names[0], names[1], names.Count - 2),
            };
        }
    }

    /// <summary>
    /// Gets the full list of the required permissions for the tooltip.
    /// </summary>
    private string RequiredFullText => string.Join(", ", Permissions.Select(TitleOf));

    /// <summary>
    /// Gets the title of a permission in the interface language.
    /// </summary>
    /// <param name="code">The permission code.</param>
    /// <returns>The title, or the code of an unknown permission.</returns>
    private static string TitleOf(string code)
        => KOD.Domain.Access.Permissions.All.FirstOrDefault(p => p.Code == code) is { } permission ? Texts.T(permission.Title) : code;
}
