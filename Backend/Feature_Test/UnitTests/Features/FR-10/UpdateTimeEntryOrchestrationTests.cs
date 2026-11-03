using Domain.ValueObjects;

namespace UnitTests.Features.FR10;

public class UpdateTimeEntryOrchestrationTests
{
    // The handler forwards the full correction to the returned entry and keeps its review history.
    [Fact]
    public async Task ReturnedEntry_OwnerUpdate_AppliesCommandValuesAndReopensDraft()
    {
        var world = new UpdateTimeEntryWorld();
        var review = world.ReturnEntry();
        var command = world.Command();

        var result = await world.Handler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(world.Entry.Id, Assert.Single(world.Entries.RequestedIds));
        Assert.Equal(command.Date, world.Entry.Date);
        Assert.Equal(command.Hours, world.Entry.Hours);
        Assert.Equal("Corrected work", world.Entry.Comment);
        Assert.Equal(TimeEntryStatus.Draft, world.Entry.Status);
        Assert.Same(review, Assert.Single(world.Entry.Reviews));
    }

    // Passing a manager's ID instead of the entry owner's ID must not change a returned entry.
    [Fact]
    public async Task ReturnedEntry_NonOwnerUpdate_ReturnsOwnerMismatchWithoutMutation()
    {
        var world = new UpdateTimeEntryWorld();
        world.ReturnEntry();
        var before = world.Capture();

        var result = await world.Handler().HandleAsync(world.Command() with
        {
            EmployeeId = world.Manager.UserId.Value
        });

        UpdateTimeEntryWorld.AssertFailure("EntryOwnerMismatch", result);
        Assert.Equal(world.Entry.Id, Assert.Single(world.Entries.RequestedIds));
        world.AssertUnchanged(before);
    }
}
