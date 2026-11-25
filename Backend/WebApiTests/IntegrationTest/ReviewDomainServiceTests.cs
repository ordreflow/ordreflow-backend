using Domain.Aggregate;
using Domain.Services;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class ReviewDomainServiceTests
{
    [Fact]
    public void Domain_service_enforces_reviewer_permission_and_category_scope_without_a_handler()
    {
        var at = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var reviewer = Employee.Create("Reviewer", "reviewer@test.invalid", at).Value;
        var date = WorkDate.Create(new DateOnly(2026, 11, 1)).Value;
        var entry = TimeEntry.Record(EmployeeId.Create(Guid.NewGuid()).Value, TaskId.Create(Guid.NewGuid()).Value,
            date, Duration.Create(2m).Value, TimeEntryDescription.Create(null).Value,
            WorkDatePeriod.Create(date, date).Value, at).Value;
        var service = new TimeEntryReviewDomainService();
        var policy = new TimeEntryReviewPolicy(false);

        var missingGrant = service.Approve(entry, reviewer, true, policy, at.AddMinutes(1));
        Assert.Equal("ReviewForbidden", Assert.Single(missingGrant.Errors).Code);
        Assert.True(reviewer.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        var wrongCategory = service.Approve(entry, reviewer, false, policy, at.AddMinutes(1));
        Assert.Equal("ReviewScopeForbidden", Assert.Single(wrongCategory.Errors).Code);
        Assert.Single(entry.Audits);
        Assert.Empty(entry.Reviews);

        Assert.True(service.Approve(entry, reviewer, true, policy, at.AddMinutes(1)).IsSuccess);
        Assert.Equal(TimeEntryStatus.Approved, entry.CurrentStatus);
        Assert.Equal(2, entry.Audits.Count);
    }
}
