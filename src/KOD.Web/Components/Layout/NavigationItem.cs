using System.Security.Claims;
using KOD.Domain.Access;
using KOD.Domain.Localization;
using KOD.Web.Security;

namespace KOD.Web.Components.Layout;

/// <summary>
/// A main navigation entry, visible when the user has any of its permissions.
/// </summary>
/// <param name="Href">The link.</param>
/// <param name="Text">The label.</param>
/// <param name="Icon">The icon name.</param>
/// <param name="Permissions">The permissions that reveal the entry; empty means every signed-in user.</param>
/// <param name="ShowInTabBar">Whether the entry is shown in the mobile bottom tab bar.</param>
public sealed record NavigationItem(string Href, string Text, string Icon, IReadOnlyList<string> Permissions, bool ShowInTabBar)
{
    /// <summary>
    /// Gets all navigation entries in display order, labelled in the current UI language.
    /// </summary>
    public static IReadOnlyList<NavigationItem> All =>
    [
        new("/", Texts.T("Головна"), "house", [], true),
        new("/documents", Texts.T("Документи"), "file-text", [KOD.Domain.Access.Permissions.Documents.View], true),
        new("/signing", Texts.T("Підписання"), "signature", [KOD.Domain.Access.Permissions.Signing.Use], true),
        new("/news", Texts.T("Новини"), "newspaper", [KOD.Domain.Access.Permissions.News.View], true),
        new("/structure", Texts.T("Структура"), "network", [KOD.Domain.Access.Permissions.Structure.View], false),
        new(
            "/admin",
            Texts.T("Адміністрування"),
            "shield",
            [
                KOD.Domain.Access.Permissions.Users.View,
                KOD.Domain.Access.Permissions.Roles.Manage,
                KOD.Domain.Access.Permissions.Structure.Manage,
                KOD.Domain.Access.Permissions.News.Manage,
                KOD.Domain.Access.Permissions.Audit.View,
            ],
            false),
        new("/profile", Texts.T("Профіль"), "user", [], true),
        new("/help", Texts.T("Довідка"), "book-open", [], false),
    ];

    /// <summary>
    /// Returns the entries visible to the user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The visible entries.</returns>
    public static IEnumerable<NavigationItem> VisibleTo(ClaimsPrincipal user)
        => All.Where(item => item.Permissions.Count == 0 || item.Permissions.Any(user.HasPermission));
}
