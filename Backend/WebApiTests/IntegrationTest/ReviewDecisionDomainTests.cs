using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class ReviewDecisionDomainTests
{
    private static readonly DateTime Created = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Reviewed = Created.AddMinutes(1);

    [Fact]
    public void Approving_ready_entry_appends_review_and_current_audit_without_changing_the_work()
    {
        var employeeId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var entry = NewEntry(employeeId, taskId);

        var result = entry.Approve(reviewerId, Reviewed, allowSelfReview: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, entry.Revision);
        Assert.Equal(2, entry.Audits.Count);
        var audit = entry.GetCurrentAudit()!;
        Assert.Equal(TimeEntryStatus.Approved, audit.Status);
        Assert.Equal(reviewerId, audit.ActorId);
        Assert.Equal(taskId, audit.TaskId);
        Assert.Equal(new DateOnly(2026, 10, 1), audit.WorkDate);
        Assert.Equal(2.5, audit.Duration);
        Assert.Equal("Work", audit.Description);
        Assert.Equal(Reviewed, audit.AuditTime);
        var review = Assert.Single(entry.Reviews);
        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.Equal(reviewerId, review.ReviewerId);
        Assert.Null(review.Description);
        Assert.Equal(Reviewed, review.ReviewedAtUtc);
    }

    [Fact]
    public void Rejection_requires_reason_and_only_successful_decisions_are_recorded()
    {
        var entry = NewEntry(Guid.NewGuid(), Guid.NewGuid());
        var reviewerId = Guid.NewGuid();

        var invalid = entry.Reject(reviewerId, "   ", Reviewed, allowSelfReview: true);
        Assert.True(invalid.IsFailure);
        Assert.Equal("RejectReasonRequired", Assert.Single(invalid.Errors).Code);
        Assert.Equal(1, entry.Revision);
        Assert.Single(entry.Audits);
        Assert.Empty(entry.Reviews);

        var valid = entry.Reject(reviewerId, " Fix the date ", Reviewed, allowSelfReview: true);
        Assert.True(valid.IsSuccess);
        Assert.Equal(TimeEntryStatus.Rejected, entry.GetCurrentAudit()!.Status);
        Assert.Equal("Fix the date", Assert.Single(entry.Reviews).Description);
        Assert.Equal(Reviewed, Assert.Single(entry.Reviews).ReviewedAtUtc);
    }

    [Fact]
    public void Repeated_or_stale_decision_does_not_append_more_history()
    {
        var entry = NewEntry(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(entry.Approve(Guid.NewGuid(), Reviewed, allowSelfReview: true).IsSuccess);

        var repeat = entry.Reject(Guid.NewGuid(), "No", Reviewed.AddMinutes(1), allowSelfReview: true);

        Assert.True(repeat.IsFailure);
        Assert.Equal("TimeEntryNotReady", Assert.Single(repeat.Errors).Code);
        Assert.Equal(2, entry.Revision);
        Assert.Equal(2, entry.Audits.Count);
        Assert.Single(entry.Reviews);
    }

    [Fact]
    public void Self_review_can_be_allowed_or_disabled_in_one_rule()
    {
        var employeeId = Guid.NewGuid();
        var entry = NewEntry(employeeId, Guid.NewGuid());

        var denied = entry.Approve(employeeId, Reviewed, allowSelfReview: false);
        Assert.Equal("SelfReviewForbidden", Assert.Single(denied.Errors).Code);
        Assert.Empty(entry.Reviews);

        Assert.True(entry.Approve(employeeId, Reviewed, allowSelfReview: true).IsSuccess);
        Assert.Single(entry.Reviews);
    }

    [Fact]
    public void Invalid_audit_time_cannot_leave_a_review_without_an_audit()
    {
        var entry = NewEntry(Guid.NewGuid(), Guid.NewGuid());

        var result = entry.Approve(Guid.NewGuid(), Created, allowSelfReview: true);

        Assert.Equal("AuditTimeNotIncreasing", Assert.Single(result.Errors).Code);
        Assert.Single(entry.Audits);
        Assert.Empty(entry.Reviews);
        Assert.Equal(1, entry.Revision);
    }

    private static TimeEntry NewEntry(Guid employeeId, Guid taskId)
    {
        var id = Guid.NewGuid();
        var initial = TimeEntryAudit.Create(id, employeeId, taskId,
            new DateOnly(2026, 10, 1), 2.5, "Work", TimeEntryStatus.ReadyForApproval, Created).Value;
        return TimeEntry.Create(employeeId, initial).Value;
    }
}
