using Application.Handlers;
using Application.Queries;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;

namespace WebApiTests.IntegrationTest;

public sealed class WeeklyTimeEntriesHandlerTests
{
    private static readonly DateTime AuditTime = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Monday = new(2026, 11, 9);

    [Fact]
    public async Task Week_uses_current_values_excludes_deleted_and_preserves_rejection_reason()
    {
        var employeeId = EmployeeId.Create(Guid.NewGuid()).Value;
        var taskId = TaskId.Create(Guid.NewGuid()).Value;
        var rejected = Entry(employeeId, taskId, Monday);
        Assert.True(rejected.Reject(EmployeeId.Create(Guid.NewGuid()).Value, "Please correct",
            AuditTime.AddMinutes(1), allowSelfReview: false).IsSuccess);
        var movedOutside = Entry(employeeId, taskId, Monday);
        Assert.True(movedOutside.AddAudit(TimeEntryAudit.Create(movedOutside.Id, employeeId, taskId,
            WorkDate.Create(DateOnly.FromDateTime(Monday.AddDays(7))).Value,
            Duration.Create(3m).Value, TimeEntryDescription.Create("Moved").Value,
            TimeEntryStatus.ReadyForApproval, AuditTime.AddMinutes(1)).Value).IsSuccess);
        var deleted = Entry(employeeId, taskId, Monday, TimeEntryStatus.Draft);
        var date = deleted.CurrentWorkDate!;
        Assert.True(deleted.Delete(employeeId, WorkDatePeriod.Create(date, date).Value,
            AuditTime.AddMinutes(1)).IsSuccess);
        var sunday = Entry(employeeId, taskId, Monday.AddDays(6));
        var repository = new EmployeeEntriesRepository([movedOutside, deleted, sunday, rejected]);
        var handler = new GetMyTimeEntriesForWeekHandler(repository);

        var result = await handler.HandleAsync(new GetMyTimeEntriesForWeekQuery(
            employeeId.Value, Monday, Monday.AddDays(7)));

        Assert.True(result.IsSuccess);
        Assert.Equal(employeeId, repository.RequestedEmployeeId);
        Assert.Equal(new[] { rejected.Id.Value, sunday.Id.Value }, result.Value.Select(entry => entry.Id));
        Assert.Equal("Rejected", result.Value[0].Status);
        Assert.Equal("Please correct", result.Value[0].RejectionReason);
        Assert.Equal(taskId.Value, result.Value[0].TaskId);
        Assert.Equal(2.5m, result.Value[0].Hours);
    }

    [Theory]
    [InlineData(1, 7)]
    [InlineData(0, 6)]
    [InlineData(0, 8)]
    public async Task Non_week_ranges_fail_before_querying_entries(int startOffset, int endOffset)
    {
        var repository = new EmployeeEntriesRepository([]);
        var handler = new GetMyTimeEntriesForWeekHandler(repository);

        var result = await handler.HandleAsync(new GetMyTimeEntriesForWeekQuery(
            Guid.NewGuid(), Monday.AddDays(startOffset), Monday.AddDays(endOffset)));

        Assert.Equal("InvalidDateRange", Assert.Single(result.Errors).Code);
        Assert.Null(repository.RequestedEmployeeId);
    }

    private static TimeEntry Entry(EmployeeId employeeId, TaskId taskId, DateTime date,
        TimeEntryStatus status = TimeEntryStatus.ReadyForApproval)
    {
        var entryId = TimeEntryId.Create(Guid.NewGuid()).Value;
        var audit = TimeEntryAudit.Create(entryId, employeeId, taskId,
            WorkDate.Create(DateOnly.FromDateTime(date)).Value, Duration.Create(2.5m).Value,
            TimeEntryDescription.Create("Work").Value, status, AuditTime).Value;
        return TimeEntry.Create(employeeId, audit).Value;
    }

    private sealed class EmployeeEntriesRepository(IReadOnlyList<TimeEntry> entries) : ITimeEntryRepository
    {
        public EmployeeId? RequestedEmployeeId { get; private set; }

        public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(EmployeeId employeeId)
        {
            RequestedEmployeeId = employeeId;
            return Task.FromResult<IReadOnlyList<TimeEntry>>(entries.Where(entry => entry.EmployeeId == employeeId).ToArray());
        }

        public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task AddAsync(TimeEntry entry) => throw new NotSupportedException();
        public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<TimeEntry?> GetWithHistoryAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(Guid managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) => throw new NotSupportedException();
        public Task<bool> ExistsForTaskAsync(TaskId taskId) => throw new NotSupportedException();
    }
}
