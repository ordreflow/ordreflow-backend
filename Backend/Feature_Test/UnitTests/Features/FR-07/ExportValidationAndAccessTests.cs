using Core.Tools.OperationResult;

namespace UnitTests.Features.FR07;

public class ExportValidationAndAccessTests
{
    // A zero-length exclusive range is invalid even for an authorized requester.
    [Fact]
    public async Task EqualRangeBounds_ReturnInvalidDateRangeBeforeRepositoryAccess()
    {
        var world = new ExportWorld();

        var result = await world.Handler().HandleAsync(world.Query() with
        {
            ToDateExclusive = ExportWorld.From
        });

        AssertFailure("InvalidDateRange", result, world);
        Assert.Empty(world.Users.RequestedIds);
    }

    // A missing requester ID fails validation before looking up a user or entries.
    [Fact]
    public async Task EmptyRequesterId_ReturnsUserIdEmptyWithoutRepositoryAccess()
    {
        var world = new ExportWorld();

        var result = await world.Handler().HandleAsync(world.Query(Guid.Empty));

        AssertFailure("UserIdEmpty", result, world);
        Assert.Empty(world.Users.RequestedIds);
    }

    // A valid ID with no user must not trigger an export query.
    [Fact]
    public async Task MissingRequester_ReturnsExportForbiddenWithoutFetchingEntries()
    {
        var world = new ExportWorld();
        var missingId = Guid.Parse("ee91cee6-a3fb-4f2f-8143-d017bd145299");

        var result = await world.Handler().HandleAsync(world.Query(missingId));

        AssertFailure("ExportForbidden", result, world);
        Assert.Equal(missingId, Assert.Single(world.Users.RequestedIds).Value);
    }

    // A manager who has been deactivated cannot export despite retaining the manager role.
    [Fact]
    public async Task InactiveManager_ReturnsExportForbiddenWithoutFetchingEntries()
    {
        var world = new ExportWorld();
        Assert.True(world.Manager.Deactivate(world.Admin.Role, world.Admin.Status).IsSuccess);

        var result = await world.Handler().HandleAsync(world.Query());

        AssertFailure("ExportForbidden", result, world);
        Assert.Equal(world.Manager.UserId, Assert.Single(world.Users.RequestedIds));
    }

    private static void AssertFailure(string code, Result result, ExportWorld world)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
        Assert.Empty(world.Entries.Ranges);
        Assert.Empty(world.Tasks.RequestedIds);
        Assert.Empty(world.Orders.RequestedIds);
    }
}
