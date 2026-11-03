using Application.Commands;
using Application.Handlers;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Features.FR12;

public class ReturnTimeEntryTests
{
    // A scoped manager's reason is trimmed and retained in the entry's return review.
    [Fact]
    public async Task AssignedManager_ReturnsDraftWithReasonAndOneReview()
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var users = new FakeReturnUserRepository(employee, manager);
        var handler = new ReturnTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new ReturnTimeEntryCommand(
            entry.Id.Value, manager.UserId.Value, "  Needs correction  "));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(TimeEntryStatus.Returned, entry.Status);
        var review = Assert.Single(entry.Reviews);
        Assert.Equal(entry.Id, review.TimeEntryId);
        Assert.Equal(ReviewDecision.Returned, review.Decision);
        Assert.Equal("Needs correction", review.Reason);
        ReturnResubmissionTestData.AssertReviewLookups(entries, users, entry.Id, employee.UserId, manager.UserId);
    }

    // Missing or whitespace-only reasons are rejected without a status change or review record.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task EmptyReason_ReturnsRequiredErrorWithoutChangingEntry(string? reason)
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        var before = ReturnResubmissionTestData.Capture(entry);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var users = new FakeReturnUserRepository(employee, manager);
        var handler = new ReturnTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new ReturnTimeEntryCommand(
            entry.Id.Value, manager.UserId.Value, reason!));

        ReturnResubmissionTestData.AssertError("ReturnReasonRequired", result);
        ReturnResubmissionTestData.AssertUnchanged(before, entry);
        ReturnResubmissionTestData.AssertReviewLookups(entries, users, entry.Id, employee.UserId, manager.UserId);
    }

    // Review permission is enforced for return decisions as well as accept decisions.
    [Theory]
    [InlineData(UserRole.Employee, false, false, "ReviewForbidden")]
    [InlineData(UserRole.Manager, true, false, "ReviewScopeForbidden")]
    [InlineData(UserRole.Manager, false, true, "ReviewForbidden")]
    public async Task UnauthorizedReviewer_CannotReturnEntry(
        UserRole role, bool outsideScope, bool inactive, string errorCode)
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        var reviewer = outsideScope || role == UserRole.Employee
            ? ReturnResubmissionTestData.CreateUser("outsider@example.com", role)
            : manager;
        if (inactive)
            Assert.True(reviewer.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        var before = ReturnResubmissionTestData.Capture(entry);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var users = new FakeReturnUserRepository(employee, reviewer);
        var handler = new ReturnTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new ReturnTimeEntryCommand(
            entry.Id.Value, reviewer.UserId.Value, "Needs correction"));

        ReturnResubmissionTestData.AssertError(errorCode, result);
        ReturnResubmissionTestData.AssertUnchanged(before, entry);
        ReturnResubmissionTestData.AssertReviewLookups(entries, users, entry.Id, employee.UserId, reviewer.UserId);
    }

    // A manager cannot return a decision that is no longer in the draft state.
    [Theory]
    [InlineData(TimeEntryStatus.Returned)]
    [InlineData(TimeEntryStatus.Accepted)]
    [InlineData(TimeEntryStatus.Finalized)]
    public async Task NonDraftEntry_RejectsReturnWithoutAppendingAnotherReview(TimeEntryStatus status)
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        if (status == TimeEntryStatus.Returned)
            ReturnResubmissionTestData.Return(entry, employee, manager);
        else
        {
            ReturnResubmissionTestData.Accept(entry, employee, manager);
            if (status == TimeEntryStatus.Finalized)
                ReturnResubmissionTestData.Finalize(entry, employee, manager);
        }

        Assert.Equal(status, entry.Status);
        var before = ReturnResubmissionTestData.Capture(entry);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var users = new FakeReturnUserRepository(employee, manager);
        var handler = new ReturnTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new ReturnTimeEntryCommand(
            entry.Id.Value, manager.UserId.Value, "Another reason"));

        ReturnResubmissionTestData.AssertError("TimeEntryNotOpen", result);
        ReturnResubmissionTestData.AssertUnchanged(before, entry);
        ReturnResubmissionTestData.AssertReviewLookups(entries, users, entry.Id, employee.UserId, manager.UserId);
    }
}
