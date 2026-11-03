using Application.Handlers;
using Application.Queries;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace UnitTests.Features.FR02;

public class ManagerTimeEntryStatusFilterTests
{
    // A specified status includes only entries with that exact enum value.
    [Fact]
    public async Task ReturnedStatus_IncludesOnlyReturnedEntries()
    {
        var manager = ManagerTimeEntryTestData.CreateManager();
        var employee = ManagerTimeEntryTestData.CreateAssignedEmployee(manager);
        var draft = ManagerTimeEntryTestData.CreateEntry(employee, new DateTime(2026, 9, 9));
        var returned = ManagerTimeEntryTestData.CreateEntry(employee, new DateTime(2026, 9, 10));
        Return(returned, employee, manager);
        var entries = new FakeTimeEntryRepository(draft, returned);
        var handler = new GetManagerTimeEntriesHandler(entries, new FakeUserRepository(manager));

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(
            manager.UserId.Value, null, null, TimeEntryStatus.Returned));

        Assert.True(result.IsSuccess);
        Assert.Equal(returned.Id.Value, Assert.Single(result.Value).Id);
    }

    // A null status leaves the pre-scoped entries unfiltered by status.
    [Fact]
    public async Task NullStatus_IncludesEntriesOfDifferentStatuses()
    {
        var manager = ManagerTimeEntryTestData.CreateManager();
        var employee = ManagerTimeEntryTestData.CreateAssignedEmployee(manager);
        var draft = ManagerTimeEntryTestData.CreateEntry(employee, new DateTime(2026, 9, 9));
        var returned = ManagerTimeEntryTestData.CreateEntry(employee, new DateTime(2026, 9, 10));
        Return(returned, employee, manager);
        var entries = new FakeTimeEntryRepository(draft, returned);
        var handler = new GetManagerTimeEntriesHandler(entries, new FakeUserRepository(manager));

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(manager.UserId.Value, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal([draft.Id.Value, returned.Id.Value], result.Value.Select(dto => dto.Id));
    }

    private static void Return(TimeEntry entry, User employee, User manager) =>
        Assert.True(entry.Return(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status, "Needs correction").IsSuccess);
}
