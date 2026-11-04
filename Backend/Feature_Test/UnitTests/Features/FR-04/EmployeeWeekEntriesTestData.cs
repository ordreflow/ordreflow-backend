using Application.Handlers;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR04;

internal static class EmployeeWeekEntriesTestData
{
    private static readonly Guid EmployeeGuid = Guid.Parse("6b765ac9-33c5-4fd9-8c78-9f5324f3c229");
    private static readonly Guid TaskGuid = Guid.Parse("39cdeca1-cab1-411c-af02-beec3ab78b2e");

    internal static UserId CreateEmployeeId() => UserId.Create(EmployeeGuid).Value;

    internal static TimeEntry CreateEntry(UserId employeeId, DateTime date, decimal hours = 2m,
        string? comment = null, Guid? taskId = null) => TimeEntry.Create(
        employeeId, TaskId.Create(taskId ?? TaskGuid).Value, date, hours, comment).Value;

    internal static (GetMyTimeEntriesForWeekHandler Handler, FakeEmployeeWeekEntriesRepository Repository)
        CreateHandler(params TimeEntry[] preScopedEntries)
    {
        var repository = new FakeEmployeeWeekEntriesRepository(preScopedEntries);
        return (new GetMyTimeEntriesForWeekHandler(repository), repository);
    }
}

internal sealed class FakeEmployeeWeekEntriesRepository(params TimeEntry[] preScopedEntries) : ITimeEntryRepository
{
    private readonly List<UserId> requestedEmployeeIds = [];

    internal IReadOnlyList<UserId> RequestedEmployeeIds => requestedEmployeeIds;

    public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId employeeId)
    {
        requestedEmployeeIds.Add(employeeId);
        // Entries are pre-scoped; this fake does not test the real repository's ownership filtering.
        return Task.FromResult<IReadOnlyList<TimeEntry>>(preScopedEntries);
    }

    public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
    public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) => throw new NotSupportedException();
    public Task<bool> ExistsForTaskAsync(TaskId taskId) => throw new NotSupportedException();
    public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();
    public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
}
