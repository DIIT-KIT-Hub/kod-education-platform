using KOD.Domain.Access;
using KOD.Domain.Common;
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
    public void User_can_hold_several_positions_with_one_primary()
    {
        var user = new User("lecturer@university.edu.ua", "Коваленко", "Марія", "Андріївна");
        var unit = Guid.CreateVersion7();

        user.SetPositions([(unit, Guid.CreateVersion7(), "Доцент", false), (unit, Guid.CreateVersion7(), "Методист", false)]);

        Assert.Equal(2, user.Positions.Count);
        Assert.Single(user.Positions, static p => p.IsPrimary);
    }

    [Fact]
    public void Two_primary_positions_are_rejected()
    {
        var user = new User("lecturer@university.edu.ua", "Коваленко", "Марія", null);
        var unit = Guid.CreateVersion7();

        Assert.Throws<DomainException>(() => user.SetPositions([(unit, Guid.CreateVersion7(), null, true), (unit, Guid.CreateVersion7(), null, true)]));
    }

    [Fact]
    public void A_permission_cannot_be_granted_and_denied_at_once()
    {
        var user = new User("hr@university.edu.ua", "Поліщук", "Оксана", null);

        Assert.Throws<DomainException>(() => user.SetPermissionOverrides([Permissions.Users.View], [Permissions.Users.View]));
    }

    [Fact]
    public void Names_follow_the_ukrainian_order()
    {
        var user = new User("Head@University.edu.ua", "Кравченко", "Дмитро", "Олегович");

        Assert.Equal("Кравченко Дмитро Олегович", user.FullName);
        Assert.Equal("Кравченко Д. О.", user.ShortName);
        Assert.Equal("head@university.edu.ua", user.Email);
        Assert.True(user.MustChangePassword);
    }
}
