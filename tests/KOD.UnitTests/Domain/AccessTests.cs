using KOD.Domain.Access;
using KOD.Domain.Common;
using KOD.Domain.Organization;
using KOD.Domain.Users;

namespace KOD.UnitTests.Domain;

public sealed class AccessTests
{
    [Fact]
    public void Administrator_role_always_has_every_permission()
    {
        var role = new Role(SystemRoles.Administrator, "Адміністратор", null, isSystem: true);

        role.SetPermissions([Permissions.News.View]);

        Assert.Equal(Permissions.Codes.Count, role.Permissions.Count);
    }

    [Fact]
    public void Role_permissions_are_identity_role_claims_and_the_name_is_normalized()
    {
        var role = new Role("custom", "  Власна роль ", null, isSystem: false);

        role.SetPermissions([Permissions.News.View, Permissions.Documents.View]);
        role.SetPermissions([Permissions.News.View]);

        var claim = Assert.Single(role.Permissions);
        Assert.Equal(PermissionClaimTypes.Permission, claim.ClaimType);
        Assert.Equal(Permissions.News.View, claim.ClaimValue);
        Assert.Equal("Власна роль", role.Name);
        Assert.Equal("ВЛАСНА РОЛЬ", role.NormalizedName);
    }

    [Fact]
    public void Permission_overrides_are_identity_user_claims()
    {
        var user = new User("hr@example.com", "Поліщук", "Оксана", null);

        user.SetPermissionOverrides([Permissions.Users.View], [Permissions.News.Manage]);

        Assert.Contains(user.PermissionOverrides, static o => o.ClaimType == PermissionClaimTypes.Permission && o.ClaimValue == Permissions.Users.View);
        Assert.Contains(user.PermissionOverrides, static o => o.ClaimType == PermissionClaimTypes.DeniedPermission && o.ClaimValue == Permissions.News.Manage);
        Assert.All(user.PermissionOverrides, o => Assert.Equal(user.Id, o.UserId));
    }

    [Fact]
    public void Unknown_permissions_are_rejected()
    {
        var role = new Role("custom", "Власна роль", null, isSystem: false);

        Assert.Throws<DomainException>(() => role.SetPermissions(["documents.everything"]));
    }

    [Fact]
    public void Every_system_role_uses_known_permissions()
    {
        foreach (var definition in SystemRoles.All)
        {
            Assert.All(definition.Permissions, static p => Assert.True(Permissions.Codes.Contains(p), p));
        }
    }

    [Fact]
    public void Leadership_system_roles_see_beyond_own_documents()
    {
        foreach (var definition in SystemRoles.All.Where(static r => r.IsLeadership))
        {
            Assert.True(
                definition.Permissions.Contains(Permissions.Documents.ViewUnit) || definition.Permissions.Contains(Permissions.Documents.ViewOrganization),
                definition.Code);
        }
    }

    [Fact]
    public void Role_keeps_the_leadership_flag()
    {
        var role = new Role("project-manager", "Керівник проєкту", null, isSystem: false);

        role.Update("Керівник проєкту", "Веде проєкт", isLeadership: true);

        Assert.True(role.IsLeadership);
        Assert.Equal("Веде проєкт", role.Description);
    }

    [Fact]
    public void Unit_type_normalizes_the_code_and_validates_the_icon()
    {
        var type = new UnitType(" workshop ", "Цех", isOrganization: false);

        Assert.Equal("WORKSHOP", type.Code);
        Assert.Equal(UnitType.DefaultIcon, type.Icon);
        Assert.Throws<DomainException>(() => type.Update("WORKSHOP", "Цех", null, "rocket", isOrganization: false, 10));
        Assert.Throws<DomainException>(() => new UnitType("цех", "Цех", isOrganization: false));

        type.Update("workshop-1", "Цех №1", "Виробничий цех", "settings", isOrganization: false, 20);

        Assert.Equal("WORKSHOP-1", type.Code);
        Assert.Equal("settings", type.Icon);
        Assert.Equal(20, type.SortOrder);
    }

    [Fact]
    public void Organization_unit_codes_are_normalized()
    {
        var unit = new OrganizationUnit("sales-lviv", "Відділ продажу", Guid.CreateVersion7(), Guid.CreateVersion7());

        Assert.Equal("SALES-LVIV", unit.Code);
        Assert.Throws<DomainException>(() => unit.Update("продаж", "Відділ продажу", null, unit.TypeId, 0));
    }

    [Fact]
    public void User_can_hold_several_positions_with_one_primary()
    {
        var user = new User("engineer@example.com", "Коваленко", "Марія", "Андріївна");
        var unit = Guid.CreateVersion7();

        user.SetPositions([(unit, Guid.CreateVersion7(), "Інженер-програміст", false), (unit, Guid.CreateVersion7(), "Наставник", false)]);

        Assert.Equal(2, user.Positions.Count);
        Assert.Single(user.Positions, static p => p.IsPrimary);
    }

    [Fact]
    public void Two_primary_positions_are_rejected()
    {
        var user = new User("engineer@example.com", "Коваленко", "Марія", null);
        var unit = Guid.CreateVersion7();

        Assert.Throws<DomainException>(() => user.SetPositions([(unit, Guid.CreateVersion7(), null, true), (unit, Guid.CreateVersion7(), null, true)]));
    }

    [Fact]
    public void A_permission_cannot_be_granted_and_denied_at_once()
    {
        var user = new User("hr@example.com", "Поліщук", "Оксана", null);

        Assert.Throws<DomainException>(() => user.SetPermissionOverrides([Permissions.Users.View], [Permissions.Users.View]));
    }

    [Fact]
    public void Names_follow_the_ukrainian_order()
    {
        var user = new User("Head@Example.com", "Кравченко", "Дмитро", "Олегович");

        Assert.Equal("Кравченко Дмитро Олегович", user.FullName);
        Assert.Equal("Кравченко Д. О.", user.ShortName);
        Assert.Equal("head@example.com", user.Email);
        Assert.True(user.MustChangePassword);
    }
}
