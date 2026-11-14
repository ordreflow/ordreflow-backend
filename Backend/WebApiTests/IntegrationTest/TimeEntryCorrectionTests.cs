using Application.Commands;
using Application.Handlers;
using Application.Queries;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;

namespace WebApiTests.IntegrationTest;

public sealed class TimeEntryCorrectionTests
{
    private static readonly DateTime Created = new(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc);
    private static readonly WorkDate WorkDate = Domain.ValueObjects.WorkDate.Create(new DateOnly(2026, 11, 16)).Value;

    [Theory]
    [InlineData(TimeEntryStatus.Draft)]
    [InlineData(TimeEntryStatus.ReadyForApproval)]
    [InlineData(TimeEntryStatus.Rejected)]
    public void Editing_an_unapproved_entry_submits_the_latest_values_and_preserves_history(TimeEntryStatus status)
    {
        var entry = Entry(status);
        var before = entry.GetCurrentAudit()!;
        var previousRevision = entry.Revision;

        var result = entry.Change(entry.EmployeeId, WorkDate, Duration.Create(4m).Value,
            TimeEntryDescription.Create("Corrected").Value, Period(), Created.AddMinutes(2));

        Assert.True(result.IsSuccess);
        var current = entry.GetCurrentAudit()!;
        Assert.Equal(TimeEntryStatus.ReadyForApproval, current.Status);
        Assert.Equal(4m, current.Duration.Value);
        Assert.Equal("Corrected", current.Description.Value);
        Assert.Equal(before.TaskId, current.TaskId);
        Assert.Equal(entry.EmployeeId, current.ActorId);
        Assert.Contains(before, entry.Audits);
        Assert.Equal(previousRevision + 1, entry.Revision);
        Assert.Null(entry.CurrentRejectionReason);
        if (status == TimeEntryStatus.Rejected)
            Assert.Equal("Wrong hours", Assert.Single(entry.Reviews).Description!.Value);

        // Corrections are immediately reviewable without a separate resubmit call.
        Assert.True(entry.Approve(EmployeeId.Create(Guid.NewGuid()).Value,
            Created.AddMinutes(3), allowSelfReview: false).IsSuccess);
    }

    [Theory]
    [InlineData(TimeEntryStatus.Draft)]
    [InlineData(TimeEntryStatus.ReadyForApproval)]
    [InlineData(TimeEntryStatus.Rejected)]
    public void Deleting_an_unapproved_entry_appends_a_tombstone_and_preserves_history(TimeEntryStatus status)
    {
        var entry = Entry(status);
        var before = entry.GetCurrentAudit()!;
        var previousRevision = entry.Revision;

        var result = entry.Delete(entry.EmployeeId, Period(), Created.AddMinutes(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeEntryStatus.Deleted, entry.CurrentStatus);
        Assert.Contains(before, entry.Audits);
        Assert.Equal(previousRevision + 1, entry.Revision);
        if (status == TimeEntryStatus.Rejected)
            Assert.Equal("Wrong hours", Assert.Single(entry.Reviews).Description!.Value);
    }

    [Theory]
    [InlineData(TimeEntryStatus.Approved)]
    [InlineData(TimeEntryStatus.Closed)]
    [InlineData(TimeEntryStatus.Deleted)]
    public void Locked_or_deleted_entries_cannot_be_changed(TimeEntryStatus status)
    {
        var entry = Entry(status);

        var change = entry.Change(entry.EmployeeId, WorkDate, Duration.Create(4m).Value,
            TimeEntryDescription.Create("Correction").Value, Period(), Created.AddMinutes(2));
        var delete = entry.Delete(entry.EmployeeId, Period(), Created.AddMinutes(2));

        Assert.Equal("TimeEntryNotEditable", Assert.Single(change.Errors).Code);
        Assert.Equal("TimeEntryNotEditable", Assert.Single(delete.Errors).Code);
        Assert.Equal(status, entry.CurrentStatus);
        Assert.Single(entry.Audits);
        Assert.Equal(1, entry.Revision);
    }

    [Fact]
    public void Another_employee_cannot_edit_or_delete_a_ready_entry()
    {
        var entry = Entry(TimeEntryStatus.ReadyForApproval);
        var other = EmployeeId.Create(Guid.NewGuid()).Value;

        var change = entry.Change(other, WorkDate, Duration.Create(4m).Value,
            TimeEntryDescription.Create("Correction").Value, Period(), Created.AddMinutes(2));
        var delete = entry.Delete(other, Period(), Created.AddMinutes(2));

        Assert.Equal("EntryOwnerMismatch", Assert.Single(change.Errors).Code);
        Assert.Equal("EntryOwnerMismatch", Assert.Single(delete.Errors).Code);
        Assert.Single(entry.Audits);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Correction_requires_both_original_and_new_dates_inside_the_period(bool originalOutside)
    {
        var entry = Entry(TimeEntryStatus.Rejected);
        var nextDay = Domain.ValueObjects.WorkDate.Create(WorkDate.Value.AddDays(1)).Value;
        var period = originalOutside ? WorkDatePeriod.Create(nextDay, nextDay).Value : Period();

        var result = entry.Change(entry.EmployeeId, nextDay, Duration.Create(4m).Value,
            TimeEntryDescription.Create("Correction").Value, period, Created.AddMinutes(2));

        Assert.Equal("WorkDateNotPermitted", Assert.Single(result.Errors).Code);
        Assert.Equal(TimeEntryStatus.Rejected, entry.CurrentStatus);
        Assert.Equal("Wrong hours", entry.CurrentRejectionReason?.Value);
        Assert.Equal(2, entry.Revision);
    }

    [Theory]
    [InlineData("edit", true)]
    [InlineData("delete", true)]
    [InlineData("resubmit", true)]
    [InlineData("edit", false)]
    [InlineData("delete", false)]
    [InlineData("resubmit", false)]
    public async Task Writes_use_company_local_cutoff_and_capture_one_utc_instant(string action, bool localDate)
    {
        // UTC Sunday, but already Monday in the company time zone.
        var instant = new DateTimeOffset(2026, 11, 15, 23, 30, 0, TimeSpan.Zero);
        var date = localDate ? WorkDate : Domain.ValueObjects.WorkDate.Create(new DateOnly(2026, 11, 15)).Value;
        var entry = Entry(TimeEntryStatus.Rejected, date);
        var repository = new EntryRepository(entry);
        var periods = new PeriodRepository(RegistrationPeriodMode.Create(
            entry.EmployeeId, RegistrationInterval.Daily, null).Value);
        var clock = new FixedClock(instant);
        var zone = TimeZoneInfo.CreateCustomTimeZone("Company", TimeSpan.FromHours(2), "Company", "Company");
        var revision = entry.Revision;

        var result = action switch
        {
            "edit" => await new UpdateTimeEntryHandler(repository, periods, clock, zone).HandleAsync(
                new UpdateTimeEntryCommand(entry.Id.Value, entry.EmployeeId.Value,
                    date.Value.ToDateTime(TimeOnly.MinValue), 4m, "Corrected")),
            "delete" => await new DeleteTimeEntryHandler(repository, periods, clock, zone).HandleAsync(
                new DeleteTimeEntryCommand(entry.Id.Value, entry.EmployeeId.Value)),
            _ => await new ResubmitTimeEntryHandler(repository, periods, clock, zone).HandleAsync(
                new ResubmitTimeEntryCommand(entry.Id.Value, entry.EmployeeId.Value))
        };

        Assert.Equal(1, clock.Reads);
        if (localDate)
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(instant.UtcDateTime, entry.GetCurrentAudit()!.AuditTime);
            Assert.Equal(action == "delete" ? TimeEntryStatus.Deleted : TimeEntryStatus.ReadyForApproval, entry.CurrentStatus);
            Assert.Equal(revision + 1, entry.Revision);
        }
        else
        {
            Assert.Equal("WorkDateNotPermitted", Assert.Single(result.Errors).Code);
            Assert.Equal(TimeEntryStatus.Rejected, entry.CurrentStatus);
            Assert.Equal(revision, entry.Revision);
        }
    }

    [Fact]
    public async Task Correcting_rejected_entry_clears_current_reason_but_owner_can_still_read_review_history()
    {
        var entry = Entry(TimeEntryStatus.Rejected);
        Assert.True(entry.Change(entry.EmployeeId, WorkDate, Duration.Create(4m).Value,
            TimeEntryDescription.Create("Corrected").Value, Period(), Created.AddMinutes(2)).IsSuccess);
        var repository = new EntryRepository(entry);
        var history = new GetTimeEntryReviewsHandler(repository);

        var result = await history.HandleAsync(new GetTimeEntryReviewsQuery(entry.EmployeeId.Value, entry.Id.Value));
        var forbidden = await history.HandleAsync(new GetTimeEntryReviewsQuery(Guid.NewGuid(), entry.Id.Value));
        var current = await new GetTimeEntryHandler(repository).HandleAsync(
            new GetTimeEntryQuery(entry.EmployeeId.Value, entry.Id.Value));

        Assert.True(result.IsSuccess);
        var review = Assert.Single(result.Value);
        Assert.Equal("Wrong hours", review.Reason);
        Assert.Equal("Rejected", review.Decision);
        Assert.Equal("TimeEntryAccessForbidden", Assert.Single(forbidden.Errors).Code);
        Assert.True(current.IsSuccess);
        Assert.Equal("ReadyForApproval", current.Value.Status);
        Assert.Null(current.Value.RejectionReason);
    }

    private static WorkDatePeriod Period() => WorkDatePeriod.Create(WorkDate, WorkDate).Value;

    private static TimeEntry Entry(TimeEntryStatus status, WorkDate? date = null)
    {
        var employee = EmployeeId.Create(Guid.NewGuid()).Value;
        var id = TimeEntryId.Create(Guid.NewGuid()).Value;
        var audit = TimeEntryAudit.Create(id, employee, TaskId.Create(Guid.NewGuid()).Value,
            date ?? WorkDate, Duration.Create(2m).Value, TimeEntryDescription.Create("Original").Value,
            status == TimeEntryStatus.Rejected ? TimeEntryStatus.ReadyForApproval : status, Created).Value;
        var entry = TimeEntry.Create(employee, audit).Value;
        if (status == TimeEntryStatus.Rejected)
            Assert.True(entry.Reject(EmployeeId.Create(Guid.NewGuid()).Value, "Wrong hours",
                Created.AddMinutes(1), allowSelfReview: false).IsSuccess);
        return entry;
    }

    private sealed class FixedClock(DateTimeOffset instant) : TimeProvider
    {
        public int Reads { get; private set; }
        public override DateTimeOffset GetUtcNow()
        {
            Reads++;
            return instant;
        }
    }

    private sealed class PeriodRepository(RegistrationPeriodMode mode) : IRegistrationPeriodModeRepository
    {
        public Task<RegistrationPeriodMode?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<RegistrationPeriodMode?>(mode);
    }

    private sealed class EntryRepository(TimeEntry entry) : ITimeEntryRepository
    {
        public Task<TimeEntry?> GetWithHistoryAsync(TimeEntryId id) =>
            Task.FromResult<TimeEntry?>(entry.Id == id ? entry : null);
        public Task<TimeEntry?> GetAsync(TimeEntryId id) => GetWithHistoryAsync(id);
        public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => GetWithHistoryAsync(id);
        public Task AddAsync(TimeEntry value) => throw new NotSupportedException();
        public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(EmployeeId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(Guid id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) => throw new NotSupportedException();
        public Task<bool> ExistsForTaskAsync(TaskId id) => throw new NotSupportedException();
    }
}
