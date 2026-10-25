using KOD.Application.Common.Access;
using KOD.Application.Common.Files;
using KOD.Application.Common.Results;
using KOD.Application.Features.News;
using KOD.Application.Features.Organization;
using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Domain.News;
using KOD.Domain.Users;

namespace KOD.UnitTests.Application;

public sealed class ScopedAdministrationTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public void Scope_covers_every_unit_or_only_its_own()
    {
        Guid inside = Guid.NewGuid(), outside = Guid.NewGuid();
        var scope = AdministrationScope.Of(new HashSet<Guid> { inside });

        Assert.True(AdministrationScope.Cabinet.Covers([]));
        Assert.False(scope.Covers([]));
        Assert.True(scope.Covers([inside]));
        Assert.False(scope.Covers([inside, outside]));
        Assert.True(scope.Overlaps([inside, outside]));
        Assert.False(scope.Overlaps([]));
        Assert.False(AdministrationScope.None.Contains(inside));
    }

    [Fact]
    public async Task Administrative_permissions_apply_only_in_the_organizations_where_a_position_grants_them()
    {
        var scope = await _world.As(_world.HrA).Scope.GetAdministrationScopeAsync(Permissions.Users.Manage, CancellationToken.None);

        // The HR position in A covers A with its nested subsidiary S; the employee position in B grants nothing there.
        Assert.False(scope.IsCabinetWide);
        Assert.True(scope.Contains(_world.A.Id));
        Assert.True(scope.Contains(_world.SDev.Id));
        Assert.False(scope.Contains(_world.B.Id));
        Assert.False(scope.Contains(_world.BProj.Id));
    }

    [Fact]
    public async Task System_administrators_administer_the_whole_cabinet()
    {
        var services = _world.As(_world.Admin);

        var scope = await services.Scope.GetAdministrationScopeAsync(Permissions.Users.Manage, CancellationToken.None);
        var users = await services.Users.ListAsync(new UserQuery { PageSize = 100 }, CancellationToken.None);

        Assert.True(scope.IsCabinetWide);
        Assert.Contains(users.Ok().Items, u => u.Id == _world.EmployeeB.Id);
    }

    [Fact]
    public async Task Without_the_permission_the_scope_is_empty()
    {
        var scope = await _world.As(_world.EmployeeA).Scope.GetAdministrationScopeAsync(Permissions.Users.View, CancellationToken.None);

        Assert.Same(AdministrationScope.None, scope);
    }

    [Fact]
    public async Task Hr_of_one_organization_lists_only_its_accounts()
    {
        var result = await _world.As(_world.HrA).Users.ListAsync(new UserQuery { PageSize = 100 }, CancellationToken.None);

        var ids = result.Ok().Items.Select(static u => u.Id).ToHashSet();
        Assert.Contains(_world.EmployeeA.Id, ids);
        Assert.Contains(_world.EmployeeS.Id, ids);
        Assert.Contains(_world.Shared.Id, ids);
        Assert.DoesNotContain(_world.EmployeeB.Id, ids);
    }

    [Fact]
    public async Task Shared_accounts_show_the_position_in_the_administrators_organization()
    {
        var result = await _world.As(_world.HrA).Users.ListAsync(new UserQuery { Search = "shared" }, CancellationToken.None);

        Assert.Equal(_world.AHr.Name, Assert.Single(result.Ok().Items).Unit);
    }

    [Fact]
    public async Task Unit_filter_includes_nested_units()
    {
        var result = await _world.As(_world.Admin).Users.ListAsync(new UserQuery { UnitId = _world.S.Id }, CancellationToken.None);

        Assert.Equal(_world.EmployeeS.Id, Assert.Single(result.Ok().Items).Id);
    }

    [Fact]
    public async Task Hr_of_one_organization_cannot_open_or_change_accounts_of_another()
    {
        var users = _world.As(_world.HrA).Users;

        var details = await users.GetAsync(_world.EmployeeB.Id, CancellationToken.None);
        var update = await users.UpdateAsync(_world.EmployeeB.Id, Command(_world.EmployeeB, new PositionInput(_world.BProj.Id, _world.Employee.Id, "Змінено", true)), CancellationToken.None);
        var block = await users.SetActiveAsync(_world.EmployeeB.Id, isActive: false, CancellationToken.None);
        var password = await users.ResetPasswordAsync(_world.EmployeeB.Id, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, details.Failure());
        Assert.Equal(ErrorType.NotFound, update.Failure());
        Assert.Equal(ErrorType.NotFound, block.Failure());
        Assert.Equal(ErrorType.NotFound, password.Failure());
    }

    [Fact]
    public async Task Positions_can_be_assigned_only_in_own_organizations()
    {
        var users = _world.As(_world.HrA).Users;

        var foreign = await users.CreateAsync(NewUser("new.b@example.com", new PositionInput(_world.BProj.Id, _world.Employee.Id, null, true)), CancellationToken.None);
        var none = await users.CreateAsync(NewUser("new.none@example.com"), CancellationToken.None);
        var own = await users.CreateAsync(NewUser("new.s@example.com", new PositionInput(_world.SDev.Id, _world.Employee.Id, null, true)), CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, foreign.Failure());
        Assert.Equal(ErrorType.Validation, none.Failure());
        Assert.True(own.IsSuccess);
    }

    [Fact]
    public async Task Details_say_what_the_administrator_may_change()
    {
        var users = _world.As(_world.HrA).Users;

        var own = (await users.GetAsync(_world.EmployeeA.Id, CancellationToken.None)).Ok();
        var shared = (await users.GetAsync(_world.Shared.Id, CancellationToken.None)).Ok();

        Assert.True(own.CanEditAccount && own.CanEditPositions && own.CanManageSecurity);
        Assert.False(shared.CanEditAccount);
        Assert.True(shared.CanEditPositions);
        Assert.False(shared.CanManageSecurity);
        Assert.Equal(_world.B.Id, Assert.Single(shared.Positions, static p => !p.IsManaged).Organization.Id);
    }

    [Fact]
    public async Task Shared_accounts_keep_their_foreign_positions_and_personal_data()
    {
        var users = _world.As(_world.HrA).Users;
        var positions = new[]
        {
            new PositionInput(_world.AHr.Id, _world.Employee.Id, "Інспектор", false),
            new PositionInput(_world.BProj.Id, _world.Employee.Id, null, true),
        };

        var renamed = await users.UpdateAsync(_world.Shared.Id, Command(_world.Shared, positions) with { LastName = "Інше" }, CancellationToken.None);
        var moved = await users.UpdateAsync(_world.Shared.Id, Command(_world.Shared, positions[0]), CancellationToken.None);
        var blocked = await users.SetActiveAsync(_world.Shared.Id, isActive: false, CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, renamed.Failure());
        Assert.True(moved.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, blocked.Failure());
        Assert.Contains(_world.Shared.Positions, p => p.OrganizationUnitId == _world.BProj.Id && p.IsPrimary);
        Assert.Contains(_world.Shared.Positions, p => p.OrganizationUnitId == _world.AHr.Id && p.Title == "Інспектор");
    }

    [Fact]
    public async Task Positions_outside_own_organizations_cannot_be_added_to_shared_accounts()
    {
        var result = await _world.As(_world.HrA).Users.UpdateAsync(
            _world.EmployeeA.Id,
            Command(_world.EmployeeA, new PositionInput(_world.AHr.Id, _world.Employee.Id, null, true), new PositionInput(_world.BProj.Id, _world.Employee.Id, null, false)),
            CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, result.Failure());
    }

    [Fact]
    public async Task Import_rejects_units_and_accounts_of_other_organizations()
    {
        _world.SheetRows =
        [
            Row(2, "new.a@example.com", "A-HR"),
            Row(3, "new.b@example.com", "B-PROJ"),
            Row(4, _world.EmployeeB.Email!, "A-HR"),
            Row(5, _world.Shared.Email!, "A-HR"),
            Row(6, "new.none@example.com", null),
        ];

        var report = (await _world.As(_world.HrA).Users.ImportAsync(File(), dryRun: true, CancellationToken.None)).Ok();

        Assert.Equal([3, 4, 5, 6], report.Errors.Select(static e => e.Row).Order());
    }

    [Fact]
    public async Task Import_applies_rows_of_own_organizations()
    {
        _world.SheetRows = [Row(2, "new.a@example.com", "A-HR"), Row(3, _world.EmployeeA.Email!, "S-DEV")];

        var report = (await _world.As(_world.HrA).Users.ImportAsync(File(), dryRun: false, CancellationToken.None)).Ok();

        Assert.True(report.Applied);
        Assert.Equal((1, 1), (report.UsersToCreate, report.UsersToUpdate));
        Assert.Equal(_world.SDev.Id, Assert.Single(_world.EmployeeA.Positions).OrganizationUnitId);
    }

    [Fact]
    public async Task Structure_managers_change_only_their_organizations()
    {
        var organization = _world.As(_world.StewardA).Organization;

        var topLevel = await organization.CreateAsync(Unit("NEW-ORG", _world.OrganizationType.Id, null), CancellationToken.None);
        var inForeign = await organization.CreateAsync(Unit("NEW-B", _world.DivisionType.Id, _world.B.Id), CancellationToken.None);
        var inOwn = await organization.CreateAsync(Unit("NEW-A", _world.DivisionType.Id, _world.A.Id), CancellationToken.None);
        var editForeign = await organization.UpdateAsync(_world.BProj.Id, Unit("B-PROJ", _world.DivisionType.Id, _world.B.Id), CancellationToken.None);
        var moveOut = await organization.UpdateAsync(_world.AHr.Id, Unit("A-HR", _world.DivisionType.Id, _world.B.Id), CancellationToken.None);
        var demote = await organization.UpdateAsync(_world.S.Id, Unit("S", _world.DivisionType.Id, _world.A.Id), CancellationToken.None);
        var rename = await organization.UpdateAsync(_world.A.Id, Unit("A", _world.OrganizationType.Id, null) with { Name = "Нова назва" }, CancellationToken.None);

        Assert.Equal(ErrorType.Forbidden, topLevel.Failure());
        Assert.Equal(ErrorType.Forbidden, inForeign.Failure());
        Assert.True(inOwn.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, editForeign.Failure());
        Assert.Equal(ErrorType.Forbidden, moveOut.Failure());
        Assert.Equal(ErrorType.Forbidden, demote.Failure());
        Assert.True(rename.IsSuccess);
    }

    [Fact]
    public async Task Cabinet_wide_administrators_create_top_level_organizations()
    {
        var result = await _world.As(_world.Admin).Organization.CreateAsync(Unit("NEW-ORG", _world.OrganizationType.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Structure_grid_and_options_are_limited_to_own_organizations()
    {
        var organization = _world.As(_world.StewardA).Organization;

        var list = await organization.ListAsync(new UnitQuery { PageSize = 100 }, CancellationToken.None);
        var options = await organization.GetOptionsAsync(Permissions.Structure.Manage, CancellationToken.None);
        var details = await organization.GetAsync(_world.BProj.Id, CancellationToken.None);

        Assert.DoesNotContain(list.Ok().Items, u => u.Id == _world.B.Id || u.Id == _world.BProj.Id);
        Assert.Equal([_world.A.Id], options.Where(static o => o.Depth == 0).Select(static o => o.Id));
        Assert.False(details.Ok().CanManage);
    }

    [Fact]
    public async Task News_managers_publish_only_for_their_organizations()
    {
        var foreign = _world.News("Новина B", _world.B);
        var cabinet = _world.News("Новина кабінету", null);
        var own = _world.News("Новина A", _world.A);
        var news = _world.As(_world.StewardA).News;

        var created = await news.CreateAsync(Article(_world.S.Id), CancellationToken.None);
        var forCabinet = await news.CreateAsync(Article(null), CancellationToken.None);
        var forB = await news.CreateAsync(Article(_world.B.Id), CancellationToken.None);
        var editForeign = await news.UpdateAsync(foreign.Id, Article(_world.A.Id), CancellationToken.None);
        var deleteCabinet = await news.DeleteAsync(cabinet.Id, CancellationToken.None);
        var managed = await news.ListAsync(new NewsQuery { IncludeDrafts = true, PageSize = 100 }, CancellationToken.None);
        var read = await news.GetBySlugAsync(foreign.Slug, CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, forCabinet.Failure());
        Assert.Equal(ErrorType.Forbidden, forB.Failure());
        Assert.Equal(ErrorType.Forbidden, editForeign.Failure());
        Assert.Equal(ErrorType.Forbidden, deleteCabinet.Failure());
        Assert.Equal([own.Id, created.Ok()], managed.Ok().Items.Select(static a => a.Id).Order());
        Assert.False(read.Ok().CanManage);
        Assert.Equal(_world.B.Id, read.Ok().Organization?.Id);
    }

    [Fact]
    public void Only_the_system_administrator_role_administers_every_organization()
    {
        Assert.All(
            SystemRoles.All.Where(static r => r.Code != SystemRoles.Administrator),
            static r => Assert.DoesNotContain(Permissions.Administration.AllOrganizations, r.Permissions));
    }

    /// <summary>
    /// Builds an update command that keeps the account's personal data.
    /// </summary>
    /// <param name="user">The account.</param>
    /// <param name="positions">The positions.</param>
    /// <returns>The command.</returns>
    private static SaveUserCommand Command(User user, params PositionInput[] positions)
        => new(user.Email!, user.LastName, user.FirstName, user.MiddleName, user.PhoneNumber, positions, [], []);

    /// <summary>
    /// Builds a creation command.
    /// </summary>
    /// <param name="email">The e-mail.</param>
    /// <param name="positions">The positions.</param>
    /// <returns>The command.</returns>
    private static SaveUserCommand NewUser(string email, params PositionInput[] positions)
        => new(email, "Новий", "Користувач", null, null, positions, [], []);

    /// <summary>
    /// Builds a unit command.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="typeId">The type.</param>
    /// <param name="parentId">The parent.</param>
    /// <returns>The command.</returns>
    private static SaveUnitCommand Unit(string code, Guid typeId, Guid? parentId)
        => new(code, $"Підрозділ {code}", null, typeId, parentId, 0, IsActive: true);

    /// <summary>
    /// Builds a news command.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The command.</returns>
    private static SaveNewsCommand Article(Guid? organizationId)
        => new("Заголовок", "Анонс", "Текст", NewsCategory.News, IsPinned: false, Publish: false, null, RemoveCover: false, organizationId);

    /// <summary>
    /// Builds an import file; its content is supplied by the fake spreadsheet.
    /// </summary>
    /// <returns>The file.</returns>
    private static FileUpload File() => new(Stream.Null, "users.xlsx", "application/octet-stream", 1);

    /// <summary>
    /// Builds an import row.
    /// </summary>
    /// <param name="row">The row number.</param>
    /// <param name="email">The e-mail.</param>
    /// <param name="unitCode">The unit code, or <see langword="null"/> for no position.</param>
    /// <returns>The row.</returns>
    private UserSheetRow Row(int row, string email, string? unitCode)
        => new(row, email, "Імпорт", "Тест", null, null, unitCode, unitCode is null ? null : _world.Employee.Code, null, null);
}
