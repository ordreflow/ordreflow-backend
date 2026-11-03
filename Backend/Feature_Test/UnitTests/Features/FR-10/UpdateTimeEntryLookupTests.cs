namespace UnitTests.Features.FR10;

public class UpdateTimeEntryLookupTests
{
    // Invalid entry or employee identifiers fail before the handler looks up an entry.
    [Theory]
    [InlineData(true, "TimeEntryIdEmpty")]
    [InlineData(false, "UserIdEmpty")]
    public async Task EmptyIdentifier_ReturnsExactErrorWithoutRepositoryLookup(
        bool emptyEntryId, string errorCode)
    {
        var world = new UpdateTimeEntryWorld();
        var before = world.Capture();
        var command = emptyEntryId
            ? world.Command() with { TimeEntryId = Guid.Empty }
            : world.Command() with { EmployeeId = Guid.Empty };

        var result = await world.Handler().HandleAsync(command);

        UpdateTimeEntryWorld.AssertFailure(errorCode, result);
        Assert.Empty(world.Entries.RequestedIds);
        world.AssertUnchanged(before);
    }

    // A valid but unknown time-entry ID returns not found rather than editing any other entry.
    [Fact]
    public async Task UnknownEntry_ReturnsTimeEntryNotFoundWithoutMutation()
    {
        var world = new UpdateTimeEntryWorld();
        var before = world.Capture();
        var missingId = Guid.Parse("a2da2b36-5fb2-4a05-9694-76348150d51e");

        var result = await world.Handler().HandleAsync(world.Command() with { TimeEntryId = missingId });

        UpdateTimeEntryWorld.AssertFailure("TimeEntryNotFound", result);
        Assert.Equal(missingId, Assert.Single(world.Entries.RequestedIds).Value);
        world.AssertUnchanged(before);
    }
}
