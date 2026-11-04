using Application.Commands;
using Application.Handlers;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Features.FR03;

public class AcceptTimeEntryTests
{
    // An assigned active manager or an active admin accepts an open entry and records one decision.
    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task AuthorizedReviewer_AcceptsDraftAndRecordsReview(UserRole role)
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        var reviewer = role == UserRole.Manager
            ? manager
            : ApprovalTestData.CreateUser("admin@example.com", UserRole.Admin);
        var entries = new FakeApprovalEntryRepository(entry.Id, entry);
        var users = new FakeApprovalUserRepository(employee, reviewer);
        var handler = new AcceptTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new AcceptTimeEntryCommand(entry.Id.Value, reviewer.UserId.Value));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(TimeEntryStatus.Accepted, entry.Status);
        var review = Assert.Single(entry.Reviews);
        Assert.Equal(entry.Id, review.TimeEntryId);
        Assert.Equal(ReviewDecision.Accepted, review.Decision);
        Assert.Null(review.Reason);
        ApprovalTestData.AssertLookups(entries, users, entry.Id, employee.UserId, reviewer.UserId);
    }

    // Employees, out-of-scope managers, and inactive managers cannot accept someone else's entry.
    [Theory]
    [InlineData(UserRole.Employee, false, false, "ReviewForbidden")]
    [InlineData(UserRole.Manager, true, false, "ReviewScopeForbidden")]
    [InlineData(UserRole.Manager, false, true, "ReviewForbidden")]
    public async Task UnauthorizedReviewer_LeavesDraftAndHistoryUnchanged(
        UserRole role, bool outsideScope, bool inactive, string errorCode)
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        var reviewer = outsideScope || role == UserRole.Employee
            ? ApprovalTestData.CreateUser("outsider@example.com", role)
            : manager;
        if (inactive)
            Assert.True(reviewer.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        var before = ApprovalTestData.Capture(entry);
        var entries = new FakeApprovalEntryRepository(entry.Id, entry);
        var users = new FakeApprovalUserRepository(employee, reviewer);
        var handler = new AcceptTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new AcceptTimeEntryCommand(entry.Id.Value, reviewer.UserId.Value));

        ApprovalTestData.AssertError(errorCode, result);
        ApprovalTestData.AssertUnchanged(before, entry);
        ApprovalTestData.AssertLookups(entries, users, entry.Id, employee.UserId, reviewer.UserId);
    }

    // Only a draft is open for acceptance; a failed repeat or late decision adds no review.
    [Theory]
    [InlineData(TimeEntryStatus.Returned)]
    [InlineData(TimeEntryStatus.Accepted)]
    [InlineData(TimeEntryStatus.Finalized)]
    public async Task NonDraftEntry_RejectsAcceptanceWithoutChangingHistory(TimeEntryStatus status)
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        if (status == TimeEntryStatus.Returned)
            ApprovalTestData.Return(entry, employee, manager);
        else
        {
            ApprovalTestData.Accept(entry, employee, manager);
            if (status == TimeEntryStatus.Finalized)
                ApprovalTestData.Finalize(entry, employee, manager);
        }

        Assert.Equal(status, entry.Status);
        var before = ApprovalTestData.Capture(entry);
        var entries = new FakeApprovalEntryRepository(entry.Id, entry);
        var users = new FakeApprovalUserRepository(employee, manager);
        var handler = new AcceptTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new AcceptTimeEntryCommand(entry.Id.Value, manager.UserId.Value));

        ApprovalTestData.AssertError("TimeEntryNotOpen", result);
        ApprovalTestData.AssertUnchanged(before, entry);
        ApprovalTestData.AssertLookups(entries, users, entry.Id, employee.UserId, manager.UserId);
    }

    // The aggregate rejects a review against a different employee even if manager scope would match.
    [Fact]
    public void EmployeeMismatch_CannotAcceptOrAppendReview()
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        var anotherEmployee = ApprovalTestData.CreateUser("another@example.com", UserRole.Employee);
        var before = ApprovalTestData.Capture(entry);

        var result = entry.Accept(manager.UserId, anotherEmployee.UserId, employee.ManagerId,
            manager.Role, manager.Status);

        ApprovalTestData.AssertError("EmployeeMismatch", result);
        ApprovalTestData.AssertUnchanged(before, entry);
    }
}
