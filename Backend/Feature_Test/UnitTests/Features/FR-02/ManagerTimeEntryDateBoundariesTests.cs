using Application.Handlers;
using Application.Queries;

namespace UnitTests.Features.FR02;

public class ManagerTimeEntryDateBoundariesTests
{
    // With only a lower bound, the start date is included and earlier dates are excluded.
    [Fact]
    public async Task FromDateOnly_IncludesStartDateAndLaterEntries()
    {
        var manager = ManagerTimeEntryTestData.CreateManager();
        var employee = ManagerTimeEntryTestData.CreateAssignedEmployee(manager);
        var from = new DateTime(2026, 9, 9);
        var before = ManagerTimeEntryTestData.CreateEntry(employee, from.AddDays(-1));
        var at = ManagerTimeEntryTestData.CreateEntry(employee, from);
        var later = ManagerTimeEntryTestData.CreateEntry(employee, from.AddDays(1));
        var entries = new FakeTimeEntryRepository(before, at, later);
        var handler = new GetManagerTimeEntriesHandler(entries, new FakeUserRepository(manager));

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(manager.UserId.Value, from, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal([at.Id.Value, later.Id.Value], result.Value.Select(dto => dto.Id));
    }

    // With only an upper bound, the day before is included and the end date is excluded.
    [Fact]
    public async Task ToDateOnly_ExcludesEndDateAndIncludesEarlierEntries()
    {
        var manager = ManagerTimeEntryTestData.CreateManager();
        var employee = ManagerTimeEntryTestData.CreateAssignedEmployee(manager);
        var toExclusive = new DateTime(2026, 9, 10);
        var older = ManagerTimeEntryTestData.CreateEntry(employee, toExclusive.AddDays(-2));
        var before = ManagerTimeEntryTestData.CreateEntry(employee, toExclusive.AddDays(-1));
        var at = ManagerTimeEntryTestData.CreateEntry(employee, toExclusive);
        var after = ManagerTimeEntryTestData.CreateEntry(employee, toExclusive.AddDays(1));
        var entries = new FakeTimeEntryRepository(older, before, at, after);
        var handler = new GetManagerTimeEntriesHandler(entries, new FakeUserRepository(manager));

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(manager.UserId.Value, null, toExclusive, null));

        Assert.True(result.IsSuccess);
        Assert.Equal([older.Id.Value, before.Id.Value], result.Value.Select(dto => dto.Id));
    }

    // When both dates are supplied, entries must fall inside the half-open interval.
    [Fact]
    public async Task BothDates_IncludeStartAndExcludeEnd()
    {
        var manager = ManagerTimeEntryTestData.CreateManager();
        var employee = ManagerTimeEntryTestData.CreateAssignedEmployee(manager);
        var from = new DateTime(2026, 9, 9);
        var toExclusive = from.AddDays(2);
        var before = ManagerTimeEntryTestData.CreateEntry(employee, from.AddDays(-1));
        var atStart = ManagerTimeEntryTestData.CreateEntry(employee, from);
        var inside = ManagerTimeEntryTestData.CreateEntry(employee, from.AddDays(1));
        var atEnd = ManagerTimeEntryTestData.CreateEntry(employee, toExclusive);
        var entries = new FakeTimeEntryRepository(before, atStart, inside, atEnd);
        var handler = new GetManagerTimeEntriesHandler(entries, new FakeUserRepository(manager));

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(
            manager.UserId.Value, from, toExclusive, null));

        Assert.True(result.IsSuccess);
        Assert.Equal([atStart.Id.Value, inside.Id.Value], result.Value.Select(dto => dto.Id));
    }
}
