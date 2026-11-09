using ODC.Web.Features.Admin.Roles;
using ODC.Web.Features.Admin.Users;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Paging;

namespace ODC.IntegrationTests.Clients;

/// <summary>
/// User administration through the API (the administrator's forms).
/// </summary>
public static class People
{
    /// <summary>
    /// Creates an account waiting for activation with one position.
    /// </summary>
    /// <param name="admin">A client allowed to manage users in that unit.</param>
    /// <param name="email">The e-mail.</param>
    /// <param name="unitCode">The code of the unit.</param>
    /// <param name="roleCode">The code of the role.</param>
    /// <returns>The new user's identifier.</returns>
    public static async Task<Guid> CreateUserAsync(ApiClient admin, string email, string unitCode, string roleCode)
    {
        ArgumentNullException.ThrowIfNull(admin);
        var units = await admin.GetAsync<List<UnitOption>>("/api/v1/users/units");
        var roles = await admin.GetAsync<List<RoleOption>>("/api/v1/users/roles");
        var unit = units.Single(u => u.Code == unitCode);
        var role = roles.Single(r => r.Code == roleCode);

        var created = await admin.PostAsync<UserIdResponse>(
            "/api/v1/users",
            new SaveUserCommand(email, "Тестовий", "Користувач", null, null, [new PositionInput(unit.Id, role.Id, "Працівник", IsPrimary: true)], [], []));
        return created.Id;
    }

    /// <summary>
    /// Finds a user in the list the client may see.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="email">The e-mail.</param>
    /// <returns>The list item, or null when the client does not see the user.</returns>
    public static async Task<UserListItem?> FindAsync(ApiClient client, string email)
    {
        ArgumentNullException.ThrowIfNull(client);
        var page = await client.GetAsync<PagedResult<UserListItem>>($"/api/v1/users?search={Uri.EscapeDataString(email)}");
        return page.Items.SingleOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }
}
