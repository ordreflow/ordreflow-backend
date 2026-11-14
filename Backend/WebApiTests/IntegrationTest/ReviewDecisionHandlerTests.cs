using Application.Commands;
using Application.Handlers;
using Domain.Aggregate;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class ReviewDecisionHandlerTests
{
    private static readonly DateTime FirstAuditTime = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReviewTime = FirstAuditTime.AddDays(1);

    [Fact]
    public async Task Missing_reviewer_permission_stops_before_reading_entry_or_scope()
    {
        var reviewer = Employee.Create("Reviewer", "reviewer@test.invalid", DateTime.UtcNow).Value;
        var entry = NewEntry(NewEmployeeId(), NewTaskId());
        var repository = new FakeDecisionRepository(entry, entry.GetCurrentAudit()!.TaskId);
        var coordinator = Coordinator(reviewer, repository);

        var result = await new ApproveTimeEntryHandler(coordinator).HandleAsync(
            new ApproveTimeEntryCommand(entry.Id.Value, reviewer.Id.Value));

        Assert.Equal("ReviewForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, repository.EntryLookups);
        Assert.Equal(0, repository.ScopeLookups);
        Assert.Empty(entry.Reviews);
    }

    [Fact]
    public async Task Entry_moved_to_unassigned_task_cannot_be_reviewed()
    {
        var reviewer = Reviewer();
        var assignedTask = NewTaskId();
        var otherTask = NewTaskId();
        var entry = NewEntry(NewEmployeeId(), assignedTask);
        Assert.True(entry.AddAudit(TimeEntryAudit.Create(entry.Id, entry.EmployeeId, otherTask,
            WorkDate.Create(new DateOnly(2026, 10, 1)).Value, Duration.Create(2m).Value,
            TimeEntryDescription.Create("Work").Value, TimeEntryStatus.ReadyForApproval,
            FirstAuditTime.AddMinutes(1)).Value).IsSuccess);
        var repository = new FakeDecisionRepository(entry, assignedTask);

        var result = await new ApproveTimeEntryHandler(Coordinator(reviewer, repository)).HandleAsync(
            new ApproveTimeEntryCommand(entry.Id.Value, reviewer.Id.Value));

        Assert.Equal("ReviewScopeForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(otherTask, repository.LastCheckedTaskId);
        Assert.Empty(entry.Reviews);
        Assert.Equal(2, entry.Audits.Count);
    }

    [Fact]
    public async Task Approve_and_reject_use_the_current_task_and_record_utc_decisions()
    {
        var reviewer = Reviewer();
        var taskId = NewTaskId();
        var first = NewEntry(NewEmployeeId(), taskId);
        var second = NewEntry(NewEmployeeId(), taskId);

        var approved = await new ApproveTimeEntryHandler(
            Coordinator(reviewer, new FakeDecisionRepository(first, taskId)))
            .HandleAsync(new ApproveTimeEntryCommand(first.Id.Value, reviewer.Id.Value));
        var rejected = await new RejectTimeEntryHandler(
            Coordinator(reviewer, new FakeDecisionRepository(second, taskId)))
            .HandleAsync(new RejectTimeEntryCommand(second.Id.Value, reviewer.Id.Value, " Fix hours "));

        Assert.True(approved.IsSuccess);
        Assert.True(rejected.IsSuccess);
        Assert.Equal(TimeEntryStatus.Approved, first.GetCurrentAudit()!.Status);
        Assert.Equal(TimeEntryStatus.Rejected, second.GetCurrentAudit()!.Status);
        Assert.Equal("Fix hours", Assert.Single(second.Reviews).Description!.Value);
        Assert.Equal(ReviewTime, Assert.Single(first.Reviews).ReviewedAtUtc);
        Assert.Equal(ReviewTime, Assert.Single(second.Reviews).ReviewedAtUtc);
        Assert.Equal(reviewer.Id, first.GetCurrentAudit()!.ActorId);
    }

    [Fact]
    public async Task Shared_self_review_setting_applies_to_both_decisions()
    {
        var reviewer = Reviewer();
        var taskId = NewTaskId();
        var first = NewEntry(reviewer.Id, taskId);
        var second = NewEntry(reviewer.Id, taskId);

        var denied = await new ApproveTimeEntryHandler(
            Coordinator(reviewer, new FakeDecisionRepository(first, taskId), allowSelfReview: false))
            .HandleAsync(new ApproveTimeEntryCommand(first.Id.Value, reviewer.Id.Value));
        var allowed = await new RejectTimeEntryHandler(
            Coordinator(reviewer, new FakeDecisionRepository(second, taskId), allowSelfReview: true))
            .HandleAsync(new RejectTimeEntryCommand(second.Id.Value, reviewer.Id.Value, "Reason"));

        Assert.Equal("SelfReviewForbidden", Assert.Single(denied.Errors).Code);
        Assert.Empty(first.Reviews);
        Assert.True(allowed.IsSuccess);
        Assert.Equal(TimeEntryStatus.Rejected, second.GetCurrentAudit()!.Status);
    }

    private static TimeEntryReviewCoordinator Coordinator(
        Employee reviewer, FakeDecisionRepository entries, bool allowSelfReview = true) =>
        new(new FakeEmployeeRepository(reviewer), entries,
            new TimeEntryReviewPolicy(allowSelfReview), new FixedTimeProvider());

    private static Employee Reviewer()
    {
        var reviewer = Employee.Create("Reviewer", $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
        Assert.True(reviewer.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        return reviewer;
    }

    private static EmployeeId NewEmployeeId() => EmployeeId.Create(Guid.NewGuid()).Value;
    private static TaskId NewTaskId() => TaskId.Create(Guid.NewGuid()).Value;

    private static TimeEntry NewEntry(EmployeeId employeeId, TaskId taskId)
    {
        var id = TimeEntryId.Create(Guid.NewGuid()).Value;
        var audit = TimeEntryAudit.Create(id, employeeId, taskId,
            WorkDate.Create(new DateOnly(2026, 10, 1)).Value,
            Duration.Create(2m).Value, TimeEntryDescription.Create("Work").Value,
            TimeEntryStatus.ReadyForApproval, FirstAuditTime).Value;
        return TimeEntry.Create(employeeId, audit).Value;
    }

    private sealed class FakeEmployeeRepository(Employee reviewer) : IEmployeeAccessRepository
    {
        public Task<Employee?> GetWithAccessesAsync(EmployeeId employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Employee?>(reviewer.Id == employeeId ? reviewer : null);
    }

    private sealed class FakeDecisionRepository(TimeEntry entry, params TaskId[] allowedTasks) : IReviewDecisionRepository
    {
        public int EntryLookups { get; private set; }
        public int ScopeLookups { get; private set; }
        public TaskId LastCheckedTaskId { get; private set; } = null!;

        public Task<TimeEntry?> GetForDecisionAsync(TimeEntryId entryId, CancellationToken cancellationToken = default)
        {
            EntryLookups++;
            return Task.FromResult<TimeEntry?>(entryId == entry.Id ? entry : null);
        }

        public Task<bool> HasReviewerTaskAccessAsync(
            EmployeeId reviewerId, TaskId taskId, CancellationToken cancellationToken = default)
        {
            ScopeLookups++;
            LastCheckedTaskId = taskId;
            return Task.FromResult(allowedTasks.Contains(taskId));
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(ReviewTime, TimeSpan.Zero);
    }
}
