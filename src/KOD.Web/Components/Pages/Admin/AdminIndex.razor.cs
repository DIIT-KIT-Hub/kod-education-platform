using System.Security.Claims;
using KOD.Domain.Access;
using KOD.Domain.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages.Admin;

/// <summary>
/// The administration hub; shows only the sections the user has permissions for.
/// </summary>
public partial class AdminIndex
{
    /// <summary>
    /// Gets the administration sections (in the interface language).
    /// </summary>
    private static AdminSection[] Sections =>
    [
        new("/admin/users", Texts.T("Користувачі"), Texts.T("Облікові записи, посади, блокування, скидання пароля та 2FA."), "users", [Permissions.Users.View]),
        new("/admin/users/import", Texts.T("Імпорт і експорт"), Texts.T("Масове додавання користувачів з XLSX/CSV та вивантаження."), "file-spreadsheet", [Permissions.Users.Import, Permissions.Users.Export]),
        new("/admin/roles", Texts.T("Ролі та дозволи"), Texts.T("Набори дозволів для посад і модулів."), "shield", [Permissions.Roles.Manage]),
        new("/admin/units", Texts.T("Структура"), Texts.T("Організації та їхні підрозділи: департаменти, відділи, філії."), "network", [Permissions.Structure.Manage]),
        new("/admin/unit-types", Texts.T("Типи підрозділів"), Texts.T("Назви рівнів структури: організація, департамент, відділ, сектор…"), "layers", [Permissions.Structure.Manage]),
        new("/news/manage", Texts.T("Новини"), Texts.T("Публікації та оголошення."), "newspaper", [Permissions.News.Manage]),
        new("/admin/audit", Texts.T("Журнал аудиту"), Texts.T("Входи, зміни прав, підписання документів."), "history", [Permissions.Audit.View]),
    ];

    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current user.
    /// </summary>
    private ClaimsPrincipal User { get; set; } = new();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => User = (await AuthenticationState).User;

    /// <summary>
    /// An administration section.
    /// </summary>
    /// <param name="Href">The link.</param>
    /// <param name="Title">The title.</param>
    /// <param name="Description">The description.</param>
    /// <param name="Icon">The icon.</param>
    /// <param name="Permissions">The permissions that reveal the section.</param>
    private sealed record AdminSection(string Href, string Title, string Description, string Icon, string[] Permissions);
}
