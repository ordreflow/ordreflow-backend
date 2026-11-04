namespace UnitTests.Features.FR07;

public class ExportMissingReferencesTests
{
    // An admin can still export a valid accepted entry when its employee and task lookups are missing.
    [Fact]
    public async Task MissingEmployeeAndTask_ExportsRowWithBlankNames()
    {
        var world = new ExportWorld();
        var entry = world.CreateAcceptedEntry();
        world.Users.Stored.Remove(world.Employee);

        var result = await world.Handler().HandleAsync(world.Query(world.Admin.UserId.Value));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Value);
        Assert.Equal(entry.Id.Value, row.TimeEntryId);
        Assert.Equal(entry.EmployeeId.Value, row.EmployeeId);
        Assert.Equal(entry.TaskId.Value, row.TaskId);
        Assert.Equal(string.Empty, row.EmployeeName);
        Assert.Equal(string.Empty, row.TaskTitle);
        Assert.Equal(string.Empty, row.OrderName);
        Assert.Equal("Accepted", row.Status);
        Assert.Equal((ExportWorld.From, ExportWorld.ToExclusive), Assert.Single(world.Entries.Ranges));
        Assert.Equal([world.Admin.UserId, world.Employee.UserId], world.Users.RequestedIds);
        Assert.Equal(entry.TaskId, Assert.Single(world.Tasks.RequestedIds));
        Assert.Empty(world.Orders.RequestedIds);
    }
}
