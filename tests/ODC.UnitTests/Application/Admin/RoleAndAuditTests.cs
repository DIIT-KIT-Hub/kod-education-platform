using ODC.Web.Features.Admin.Audit;
using ODC.Web.Features.Admin.Roles;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Domain.Audit;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Text;

namespace ODC.UnitTests.Application.Admin;

/// <summary>
/// Roles and their permissions (cabinet-wide, <c>roles.manage</c>) and the audit log (<c>audit.view</c>).
/// </summary>
public sealed class RoleAndAuditTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task An_administrator_creates_a_role_with_a_lower_case_code_and_its_permissions()
    {
        var admin = _world.As(_world.Admin);

        var id = (await admin.SendAsync(new CreateRole.Command(Save(" Archivist ", "Архіваріус", Permissions.Documents.View, Permissions.Documents.Manage)))).Ok();

        var role = (await admin.SendAsync(new GetRole.Query(id))).Ok();
        Assert.Equal("archivist", role.Code);
        Assert.Equal("Архіваріус", role.Name);
        Assert.False(role.IsSystem);
        Assert.Equal(new[] { Permissions.Documents.Manage, Permissions.Documents.View }.Order(), role.Permissions.Order());
        Assert.Equal(0, role.UserCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Головний")]
    [InlineData("chief editor")]
    [InlineData("chief_editor")]
    public async Task A_role_code_holds_only_latin_letters_digits_and_dashes(string code)
    {
        var result = await _world.As(_world.Admin).SendAsync(new CreateRole.Command(Save(code, "Редактор")));

        Assert.Equal(ErrorType.Validation, result.Failure());
    }

    [Fact]
    public async Task Codes_and_names_of_roles_are_unique_ignoring_case_and_spaces()
    {
        var admin = _world.As(_world.Admin);
        (await admin.SendAsync(new CreateRole.Command(Save("editor", "Редактор")))).Ok();

        var sameCode = await admin.SendAsync(new CreateRole.Command(Save("EDITOR", "Інша назва")));
        var sameName = await admin.SendAsync(new CreateRole.Command(Save("editor-2", "  редактор ")));

        Assert.Equal(ErrorType.Conflict, sameCode.Failure());
        Assert.Equal(ErrorType.Conflict, sameName.Failure());
        Assert.Contains("редактор", sameName.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_permission_or_an_empty_name_is_refused()
    {
        var admin = _world.As(_world.Admin);

        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new CreateRole.Command(Save("x", "Хакер", "everything.all")))).Failure());
        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new CreateRole.Command(Save("y", " ")))).Failure());
    }

    [Fact]
    public async Task A_role_is_renamed_and_its_permissions_are_replaced()
    {
        var admin = _world.As(_world.Admin);
        var id = (await admin.SendAsync(new CreateRole.Command(Save("clerk", "Діловод", Permissions.Documents.View)))).Ok();

        var result = await admin.SendAsync(new UpdateRole.Command(id, new SaveRoleCommand("ignored", "Старший діловод", "Опис", [Permissions.Documents.Create], IsLeadership: true)));

        Assert.True(result.IsSuccess);
        var role = (await admin.SendAsync(new GetRole.Query(id))).Ok();
        Assert.Equal("clerk", role.Code);
        Assert.Equal("Старший діловод", role.Name);
        Assert.Equal("Опис", role.Description);
        Assert.True(role.IsLeadership);
        Assert.Equal([Permissions.Documents.Create], role.Permissions);
    }

    [Fact]
    public async Task A_role_cannot_take_the_name_of_another_one_but_keeps_its_own()
    {
        var admin = _world.As(_world.Admin);
        var id = (await admin.SendAsync(new CreateRole.Command(Save("clerk", "Діловод")))).Ok();

        Assert.Equal(ErrorType.Conflict, (await admin.SendAsync(new UpdateRole.Command(id, Save("clerk", _world.Employee.Name)))).Failure());
        Assert.True((await admin.SendAsync(new UpdateRole.Command(id, Save("clerk", "ДІЛОВОД")))).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await admin.SendAsync(new UpdateRole.Command(Guid.NewGuid(), Save("z", "Ніхто")))).Failure());
    }

    [Fact]
    public async Task The_administrator_role_always_keeps_every_permission()
    {
        var admin = _world.As(_world.Admin);

        Assert.True((await admin.SendAsync(new UpdateRole.Command(_world.Administrator.Id, Save("system-admin", "Адміністратор", Permissions.News.View)))).IsSuccess);

        var role = (await admin.SendAsync(new GetRole.Query(_world.Administrator.Id))).Ok();
        Assert.True(role.IsAdministrator);
        Assert.Equal(Permissions.Codes.Order(), role.Permissions.Order());
    }

    [Fact]
    public async Task A_role_in_use_or_a_system_role_is_not_deleted()
    {
        var admin = _world.As(_world.Admin);
        var unused = (await admin.SendAsync(new CreateRole.Command(Save("temp", "Тимчасова")))).Ok();

        Assert.Equal(ErrorType.Conflict, (await admin.SendAsync(new DeleteRole.Command(_world.Steward.Id))).Failure());
        var system = new Role("sys", "Системна", null, isSystem: true);
        _world.Master.Roles.Add(system);
        _world.Master.SaveChanges();
        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new DeleteRole.Command(system.Id))).Failure());
        Assert.Equal(ErrorType.NotFound, (await admin.SendAsync(new DeleteRole.Command(Guid.NewGuid()))).Failure());
        Assert.True((await admin.SendAsync(new DeleteRole.Command(unused))).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await admin.SendAsync(new GetRole.Query(unused))).Failure());
    }

    [Fact]
    public async Task The_role_list_counts_users_and_permissions_searches_and_sorts()
    {
        var admin = _world.As(_world.Admin);

        var all = (await admin.SendAsync(new ListRoles.Query())).Ok();
        var employee = all.Items.Single(r => r.Id == _world.Employee.Id);
        Assert.Equal(_world.Db.StaffPositions.Where(p => p.RoleId == _world.Employee.Id).Select(static p => p.UserId).Distinct().Count(), employee.UserCount);
        Assert.Equal(3, employee.PermissionCount);

        var found = (await admin.SendAsync(new ListRoles.Query { Search = "steward" })).Ok();
        Assert.Equal(_world.Steward.Id, Assert.Single(found.Items).Id);

        var byCode = (await admin.SendAsync(new ListRoles.Query { SortBy = "code", Descending = true })).Ok();
        Assert.Equal(byCode.Items.Select(static r => r.Code).OrderDescending(StringComparer.Ordinal), byCode.Items.Select(static r => r.Code));
        var options = await admin.SendAsync(new ListRoleOptions.Query());
        Assert.Equal(all.TotalCount, options.Count);
    }

    [Fact]
    public async Task Only_a_holder_of_roles_manage_works_with_roles()
    {
        var hr = _world.As(_world.HrA);

        Assert.Equal(ErrorType.Forbidden, (await hr.SendAsync(new ListRoles.Query())).Failure());
        Assert.Equal(ErrorType.Forbidden, (await hr.SendAsync(new GetRole.Query(_world.Employee.Id))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await hr.SendAsync(new CreateRole.Command(Save("x", "Х")))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await hr.SendAsync(new UpdateRole.Command(_world.Employee.Id, Save("x", "Х")))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await hr.SendAsync(new DeleteRole.Command(_world.Employee.Id))).Failure());
    }

    [Fact]
    public async Task The_audit_log_filters_by_category_kyiv_days_and_text_newest_first()
    {
        Audit(new DateTimeOffset(2026, 10, 1, 21, 30, 0, TimeSpan.Zero), AuditActions.SignIn, "Шевченко Тарас", "Пароль + автентифікатор", "10.0.0.1");
        Audit(new DateTimeOffset(2026, 10, 2, 8, 0, 0, TimeSpan.Zero), AuditActions.DocumentCreated, "Коваленко Марія", "2026/000001 «Наказ»", "10.0.0.2");
        Audit(new DateTimeOffset(2026, 10, 3, 8, 0, 0, TimeSpan.Zero), AuditActions.DocumentSigned, "Коваленко Марія", "2026/000001: CAdES", "10.0.0.3");
        var auditor = _world.As(_world.Admin);

        var all = (await auditor.SendAsync(new ListAuditEntries.Query())).Ok();
        Assert.Equal([AuditActions.DocumentSigned, AuditActions.DocumentCreated, AuditActions.SignIn], all.Items.Select(static e => e.Action));

        var documents = (await auditor.SendAsync(new ListAuditEntries.Query { Action = "documents" })).Ok();
        Assert.Equal(2, documents.TotalCount);

        // 21:30 UTC on 1 October is already 2 October in Kyiv.
        var second = (await auditor.SendAsync(new ListAuditEntries.Query { From = new DateOnly(2026, 10, 2), To = new DateOnly(2026, 10, 2) })).Ok();
        Assert.Equal(new[] { AuditActions.DocumentCreated, AuditActions.SignIn }.Order(StringComparer.Ordinal), second.Items.Select(static e => e.Action).Order(StringComparer.Ordinal));

        Assert.Single((await auditor.SendAsync(new ListAuditEntries.Query { Search = "CAdES" })).Ok().Items);
        Assert.Single((await auditor.SendAsync(new ListAuditEntries.Query { Search = "10.0.0.1" })).Ok().Items);
        Assert.Equal(2, (await auditor.SendAsync(new ListAuditEntries.Query { Search = "Коваленко" })).Ok().TotalCount);

        var oldest = (await auditor.SendAsync(new ListAuditEntries.Query { SortBy = "occurred", Descending = false })).Ok();
        Assert.Equal(AuditActions.SignIn, oldest.Items[0].Action);
    }

    [Fact]
    public async Task Only_a_holder_of_audit_view_reads_the_log()
    {
        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.HrA).SendAsync(new ListAuditEntries.Query())).Failure());
    }

    [Fact]
    public void A_seeded_role_name_is_shown_translated_but_a_custom_one_as_typed()
    {
        Assert.Equal("Працівник", StoredText.Display("Працівник"));
        Assert.Equal("Моя роль", StoredText.Display("Моя роль"));
    }

    /// <summary>
    /// Builds the data of a role.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="name">The name.</param>
    /// <param name="permissions">The permissions.</param>
    /// <returns>The data.</returns>
    private static SaveRoleCommand Save(string code, string name, params string[] permissions) => new(code, name, null, permissions, IsLeadership: false);

    /// <summary>
    /// Adds an entry to the audit log.
    /// </summary>
    /// <param name="at">The moment.</param>
    /// <param name="action">The event.</param>
    /// <param name="user">The user.</param>
    /// <param name="details">The details.</param>
    /// <param name="ip">The IP address.</param>
    private void Audit(DateTimeOffset at, string action, string user, string details, string ip)
    {
        _world.Db.AuditEntries.Add(new AuditEntry(at, action, null, user, null, null, details, ip));
        _world.Db.SaveChanges();
    }
}
