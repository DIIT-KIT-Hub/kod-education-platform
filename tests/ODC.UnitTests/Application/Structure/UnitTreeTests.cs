using ODC.Web.Features.Structure.Units;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Paging;

namespace ODC.UnitTests.Application.Structure;

/// <summary>
/// The structure page: the tree of units (inactive ones only on request, staff counts) and the staff of a unit (active
/// people only, leaders first, search and pages).
/// </summary>
public sealed class UnitTreeTests : IDisposable
{
    /// <summary>
    /// The cabinet with organizations A (with subsidiary S) and B.
    /// </summary>
    private readonly TwoOrganizations _world = new();

    public void Dispose() => _world.Dispose();

    [Fact]
    public async Task The_tree_nests_the_units_under_their_parents_with_their_staff_counts()
    {
        var tree = (await _world.As(_world.StewardA).SendAsync(new GetUnitTree.Query(false))).Ok();

        Assert.Equal(["A", "B"], tree.Select(static n => n.Code));
        var a = tree[0];
        Assert.Equal(["A-HR", "S"], a.Children.Select(static n => n.Code));
        Assert.Equal("S-DEV", Assert.Single(a.Children[1].Children).Code);
        Assert.Equal(1, a.StaffCount);
        Assert.Equal(4, a.Children[0].StaffCount);
        Assert.True(a.Type.IsOrganization);
        Assert.False(a.Children[0].Type.IsOrganization);
    }

    [Fact]
    public async Task Inactive_units_and_their_branches_are_shown_only_on_request()
    {
        _world.S.SetActive(false);
        _world.Db.SaveChanges();
        var steward = _world.As(_world.StewardA);

        var active = (await steward.SendAsync(new GetUnitTree.Query(false))).Ok();
        var all = (await steward.SendAsync(new GetUnitTree.Query(true))).Ok();

        Assert.Equal(["A-HR"], active[0].Children.Select(static n => n.Code));
        var s = Assert.Single(all[0].Children, static n => n.Code == "S");
        Assert.False(s.IsActive);
        Assert.Single(s.Children);
    }

    [Fact]
    public async Task Units_are_ordered_by_their_sort_order_before_their_names()
    {
        _world.B.Update("B", "Підрозділ B", null, _world.OrganizationType.Id, -1);
        _world.Db.SaveChanges();

        var tree = (await _world.As(_world.StewardA).SendAsync(new GetUnitTree.Query(false))).Ok();

        Assert.Equal(["B", "A"], tree.Select(static n => n.Code));
    }

    [Fact]
    public async Task Only_people_who_see_the_structure_get_the_tree_or_the_staff()
    {
        var employee = _world.As(_world.EmployeeA);

        Assert.Equal(ErrorType.Forbidden, (await employee.SendAsync(new GetUnitTree.Query(false))).Failure());
        Assert.Equal(ErrorType.Forbidden, (await employee.SendAsync(new ListUnitStaff.Query(_world.AHr.Id, new PageRequest()))).Failure());
    }

    [Fact]
    public async Task The_staff_of_a_unit_lists_its_leaders_first_then_by_name()
    {
        _world.Hr.Update(_world.Hr.Name!, null, isLeadership: true);
        _world.Master.SaveChanges();

        var staff = (await _world.As(_world.StewardA).SendAsync(new ListUnitStaff.Query(_world.AHr.Id, new PageRequest()))).Ok();

        Assert.Equal(4, staff.TotalCount);
        Assert.Equal(_world.HrA.Id, staff.Items[0].UserId);
        Assert.True(staff.Items[0].IsLeadership);
        Assert.Equal([_world.EmployeeA.Id, _world.Shared.Id, _world.StewardA.Id], staff.Items.Skip(1).Select(static s => s.UserId));
        Assert.Equal(_world.EmployeeA.Email, staff.Items[1].Email);
        Assert.False(staff.Items.Single(s => s.UserId == _world.Shared.Id).IsPrimary);
    }

    [Fact]
    public async Task Blocked_people_are_not_listed_in_the_staff()
    {
        _world.EmployeeA.SetActive(false);
        _world.Master.SaveChanges();

        var staff = (await _world.As(_world.StewardA).SendAsync(new ListUnitStaff.Query(_world.AHr.Id, new PageRequest()))).Ok();

        Assert.DoesNotContain(staff.Items, s => s.UserId == _world.EmployeeA.Id);
        Assert.Equal(3, staff.TotalCount);
    }

    [Fact]
    public async Task The_staff_is_searched_by_name_and_paged()
    {
        var steward = _world.As(_world.StewardA);

        var found = (await steward.SendAsync(new ListUnitStaff.Query(_world.AHr.Id, new PageRequest { Search = " steward " }))).Ok();
        var page = (await steward.SendAsync(new ListUnitStaff.Query(_world.AHr.Id, new PageRequest { Page = 2, PageSize = 3 }))).Ok();

        Assert.Equal(_world.StewardA.Id, Assert.Single(found.Items).UserId);
        Assert.Equal(4, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task The_staff_of_an_unknown_unit_is_not_found()
    {
        // A unit of another organization is not in this database either: the same answer.
        Assert.Equal(ErrorType.NotFound, (await _world.As(_world.StewardA).SendAsync(new ListUnitStaff.Query(Guid.NewGuid(), new PageRequest()))).Failure());
    }
}
