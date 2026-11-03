using Application.Handlers;
using Application.Queries;
using Domain.ValueObjects;

namespace UnitTests.Features.FR02;

public class ManagerTimeEntryAccessTests
{
    // An empty manager ID fails validation before either repository is consulted.
    [Fact]
    public async Task EmptyManagerId_ReturnsUserIdEmptyWithoutRepositoryCalls()
    {
        var users = new FakeUserRepository();
        var entries = new FakeTimeEntryRepository();
        var handler = new GetManagerTimeEntriesHandler(entries, users);

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(Guid.Empty, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("UserIdEmpty", Assert.Single(result.Errors).Code);
        Assert.Empty(users.RequestedIds);
        Assert.Empty(entries.RequestedIds);
    }

    // A valid ID with no matching user cannot fetch scoped entries.
    [Fact]
    public async Task MissingManager_ReturnsManagerNotFoundBeforeFetchingEntries()
    {
        var managerId = Guid.Parse("ee91cee6-a3fb-4f2f-8143-d017bd145299");
        var users = new FakeUserRepository();
        var entries = new FakeTimeEntryRepository();
        var handler = new GetManagerTimeEntriesHandler(entries, users);

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(managerId, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("ManagerNotFound", Assert.Single(result.Errors).Code);
        Assert.Equal(managerId, Assert.Single(users.RequestedIds).Value);
        Assert.Empty(entries.RequestedIds);
    }

    // An active employee cannot view the manager-scoped time-entry list.
    [Fact]
    public async Task ActiveEmployee_ReturnsReviewForbiddenBeforeFetchingEntries()
    {
        var employee = ManagerTimeEntryTestData.CreateEmployee();
        var users = new FakeUserRepository(employee);
        var entries = new FakeTimeEntryRepository();
        var handler = new GetManagerTimeEntriesHandler(entries, users);
        Assert.Equal(UserStatus.Active, employee.Status);

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(employee.UserId.Value, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("ReviewForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(employee.UserId, Assert.Single(users.RequestedIds));
        Assert.Empty(entries.RequestedIds);
    }

    // An inactive manager cannot fetch employee entries even with a valid role.
    [Fact]
    public async Task InactiveManager_ReturnsReviewForbiddenBeforeFetchingEntries()
    {
        var manager = ManagerTimeEntryTestData.CreateManager();
        Assert.True(manager.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        var users = new FakeUserRepository(manager);
        var entries = new FakeTimeEntryRepository();
        var handler = new GetManagerTimeEntriesHandler(entries, users);

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(manager.UserId.Value, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("ReviewForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(manager.UserId, Assert.Single(users.RequestedIds));
        Assert.Empty(entries.RequestedIds);
    }

    // An active manager with no scoped entries gets an empty list, using that manager's ID once.
    [Fact]
    public async Task ActiveManager_EmptyScopedRepository_ReturnsEmptyAndForwardsManagerIdOnce()
    {
        var manager = ManagerTimeEntryTestData.CreateManager();
        var users = new FakeUserRepository(manager);
        var entries = new FakeTimeEntryRepository();
        var handler = new GetManagerTimeEntriesHandler(entries, users);

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(manager.UserId.Value, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(manager.UserId, Assert.Single(users.RequestedIds));
        Assert.Equal(manager.UserId, Assert.Single(entries.RequestedIds));
    }
}
