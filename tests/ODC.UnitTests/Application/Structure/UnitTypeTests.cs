using ODC.Web.Features.Structure.UnitTypes;
using ODC.Web.Shared.Kernel;

namespace ODC.UnitTests.Application.Structure;

/// <summary>
/// The catalog of unit types: shared by every organization, so only an administrator of all organizations changes it.
/// </summary>
public sealed class UnitTypeTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task An_administrator_of_all_organizations_creates_reads_changes_and_deletes_a_type()
    {
        var admin = _world.As(_world.Admin);

        var id = (await admin.SendAsync(new CreateUnitType.Command(Save(" faculty ", "Факультет", "graduation-cap", sortOrder: 5)))).Ok();
        var created = (await admin.SendAsync(new GetUnitType.Query(id))).Ok();
        Assert.Equal("FACULTY", created.Code);
        Assert.Equal("graduation-cap", created.Icon);
        Assert.Equal(0, created.UnitCount);

        Assert.True((await admin.SendAsync(new UpdateUnitType.Command(id, Save("FACULTY", "Інститут", "landmark", sortOrder: 1)))).IsSuccess);
        var changed = (await admin.SendAsync(new GetUnitType.Query(id))).Ok();
        Assert.Equal("Інститут", changed.Name);
        Assert.Equal(1, changed.SortOrder);

        Assert.True((await admin.SendAsync(new DeleteUnitType.Command(id))).IsSuccess);
        Assert.Equal(ErrorType.NotFound, (await admin.SendAsync(new GetUnitType.Query(id))).Failure());
    }

    [Fact]
    public async Task The_list_is_ordered_and_counts_the_units_of_each_type()
    {
        var types = (await _world.As(_world.Admin).SendAsync(new ListUnitTypes.Query())).Ok();

        var organization = types.Single(t => t.Id == _world.OrganizationType.Id);
        var division = types.Single(t => t.Id == _world.DivisionType.Id);
        Assert.Equal(3, organization.UnitCount);
        Assert.Equal(3, division.UnitCount);
        Assert.True(organization.IsOrganization);
        Assert.Equal(types.OrderBy(static t => t.SortOrder).Select(static t => t.Id), types.Select(static t => t.Id));
    }

    [Fact]
    public async Task Codes_and_names_are_unique()
    {
        var admin = _world.As(_world.Admin);

        Assert.Equal(ErrorType.Conflict, (await admin.SendAsync(new CreateUnitType.Command(Save("division", "Інший")))).Failure());
        Assert.Equal(ErrorType.Conflict, (await admin.SendAsync(new CreateUnitType.Command(Save("OTHER", _world.DivisionType.Name)))).Failure());
        Assert.Equal(ErrorType.Conflict, (await admin.SendAsync(new UpdateUnitType.Command(_world.DivisionType.Id, Save("ORGANIZATION", "Відділ")))).Failure());
    }

    [Fact]
    public async Task A_type_with_an_empty_name_or_a_bad_code_is_refused()
    {
        var admin = _world.As(_world.Admin);

        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new CreateUnitType.Command(Save("SECTOR", " ")))).Failure());
        Assert.Equal(ErrorType.Validation, (await admin.SendAsync(new CreateUnitType.Command(Save(" ", "Сектор")))).Failure());
    }

    [Fact]
    public async Task A_type_of_existing_units_is_not_deleted()
    {
        var result = await _world.As(_world.Admin).SendAsync(new DeleteUnitType.Command(_world.DivisionType.Id));

        Assert.Equal(ErrorType.Conflict, result.Failure());
        Assert.Contains("(3)", result.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_type_of_top_level_organizations_stays_an_organization()
    {
        var result = await _world.As(_world.Admin).SendAsync(new UpdateUnitType.Command(_world.OrganizationType.Id, Save("ORGANIZATION", "Організація", isOrganization: false)));

        Assert.Equal(ErrorType.Conflict, result.Failure());
    }

    [Fact]
    public async Task Unknown_types_are_not_found()
    {
        var admin = _world.As(_world.Admin);

        Assert.Equal(ErrorType.NotFound, (await admin.SendAsync(new UpdateUnitType.Command(Guid.NewGuid(), Save("X", "Х")))).Failure());
        Assert.Equal(ErrorType.NotFound, (await admin.SendAsync(new DeleteUnitType.Command(Guid.NewGuid()))).Failure());
    }

    [Fact]
    public async Task A_steward_of_one_organization_reads_the_catalog_but_does_not_change_it()
    {
        var steward = _world.As(_world.StewardA);

        Assert.True((await steward.SendAsync(new ListUnitTypes.Query())).IsSuccess);
        Assert.Equal(ErrorType.Forbidden, (await steward.SendAsync(new CreateUnitType.Command(Save("SECTOR", "Сектор")))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await steward.SendAsync(new UpdateUnitType.Command(_world.DivisionType.Id, Save("DIVISION", "Відділ")))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await steward.SendAsync(new DeleteUnitType.Command(_world.DivisionType.Id))).Failure());
    }

    [Fact]
    public async Task An_employee_does_not_reach_the_catalog()
    {
        Assert.Equal(ErrorType.Forbidden, (await _world.As(_world.EmployeeA).SendAsync(new ListUnitTypes.Query())).Failure());
    }

    /// <summary>
    /// Builds the data of a type.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="name">The name.</param>
    /// <param name="icon">The Lucide icon name.</param>
    /// <param name="isOrganization">Whether units of this type are organizations.</param>
    /// <param name="sortOrder">The position in lists.</param>
    /// <returns>The data.</returns>
    private static SaveUnitTypeCommand Save(string code, string name, string? icon = null, bool isOrganization = false, int sortOrder = 10)
        => new(code, name, null, icon, isOrganization, sortOrder);
}
