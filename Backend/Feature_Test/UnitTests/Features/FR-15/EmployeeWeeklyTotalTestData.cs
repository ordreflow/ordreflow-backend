using Application.Handlers;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR15;

internal static class EmployeeWeeklyTotalTestData
{
    private static readonly Guid EmployeeGuid = Guid.Parse("583098ef-749d-4ed4-b96a-1017a5d4d5bb");
    private static readonly Guid TaskGuid = Guid.Parse("a41d03df-77dc-4ab5-ab8e-6c6024d4f4ac");

    internal static UserId CreateEmployeeId() => UserId.Create(EmployeeGuid).Value;

    internal static TimeEntry CreateEntry(UserId employeeId, DateTime date, decimal hours) => TimeEntry.Create(
        employeeId, TaskId.Create(TaskGuid).Value, date, hours, null).Value;

    internal static (GetMyWeeklyTotalHandler Handler, FakeEmployeeWeeklyTotalRepository Repository)
        CreateHandler(params TimeEntry[] preScopedEntries)
    {
        var repository = new FakeEmployeeWeeklyTotalRepository(preScopedEntries);
        return (new GetMyWeeklyTotalHandler(repository), repository);
    }
}

internal sealed class FakeEmployeeWeeklyTotalRepository(params TimeEntry[] preScopedEntries) : ITimeEntryRepository
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
