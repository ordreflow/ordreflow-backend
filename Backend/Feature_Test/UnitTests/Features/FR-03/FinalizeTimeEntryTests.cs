using Application.Commands;
using Application.Handlers;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Features.FR03;

public class FinalizeTimeEntryTests
{
    // Finalization requires prior acceptance and appends a distinct decision without replacing it.
    [Fact]
    public async Task AcceptedEntry_FinalizesAndPreservesAcceptedReview()
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        ApprovalTestData.Accept(entry, employee, manager);
        var acceptedReview = Assert.Single(entry.Reviews);
        var entries = new FakeApprovalEntryRepository(entry.Id, entry);
        var users = new FakeApprovalUserRepository(employee, manager);
        var handler = new FinalizeTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new FinalizeTimeEntryCommand(entry.Id.Value, manager.UserId.Value));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(TimeEntryStatus.Finalized, entry.Status);
        Assert.Collection(entry.Reviews,
            review =>
            {
                Assert.Same(acceptedReview, review);
                Assert.Equal(entry.Id, review.TimeEntryId);
                Assert.Equal(ReviewDecision.Accepted, review.Decision);
                Assert.Null(review.Reason);
            },
            review =>
            {
                Assert.Equal(entry.Id, review.TimeEntryId);
                Assert.Equal(ReviewDecision.Finalized, review.Decision);
                Assert.Null(review.Reason);
            });
        ApprovalTestData.AssertLookups(entries, users, entry.Id, employee.UserId, manager.UserId);
    }

    // Draft, returned and already-finalized entries cannot be finalized again.
    [Theory]
    [InlineData(TimeEntryStatus.Draft)]
    [InlineData(TimeEntryStatus.Returned)]
    [InlineData(TimeEntryStatus.Finalized)]
    public async Task EntryWithoutOutstandingAcceptance_RejectsFinalization(TimeEntryStatus status)
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        if (status == TimeEntryStatus.Returned)
            ApprovalTestData.Return(entry, employee, manager);
        else if (status == TimeEntryStatus.Finalized)
        {
            ApprovalTestData.Accept(entry, employee, manager);
            ApprovalTestData.Finalize(entry, employee, manager);
        }

        Assert.Equal(status, entry.Status);
        var before = ApprovalTestData.Capture(entry);
        var entries = new FakeApprovalEntryRepository(entry.Id, entry);
        var users = new FakeApprovalUserRepository(employee, manager);
        var handler = new FinalizeTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new FinalizeTimeEntryCommand(entry.Id.Value, manager.UserId.Value));

        ApprovalTestData.AssertError("TimeEntryNotAccepted", result);
        ApprovalTestData.AssertUnchanged(before, entry);
        ApprovalTestData.AssertLookups(entries, users, entry.Id, employee.UserId, manager.UserId);
    }

    // An accepted entry is still protected from unauthorized finalization.
    [Theory]
    [InlineData(UserRole.Employee, "ReviewForbidden")]
    [InlineData(UserRole.Manager, "ReviewScopeForbidden")]
    public async Task UnauthorizedReviewer_CannotFinalizeAcceptedEntry(UserRole role, string errorCode)
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        ApprovalTestData.Accept(entry, employee, manager);
        var outsider = ApprovalTestData.CreateUser("outsider@example.com", role);
        var before = ApprovalTestData.Capture(entry);
        var entries = new FakeApprovalEntryRepository(entry.Id, entry);
        var users = new FakeApprovalUserRepository(employee, outsider);
        var handler = new FinalizeTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new FinalizeTimeEntryCommand(entry.Id.Value, outsider.UserId.Value));

        ApprovalTestData.AssertError(errorCode, result);
        ApprovalTestData.AssertUnchanged(before, entry);
        ApprovalTestData.AssertLookups(entries, users, entry.Id, employee.UserId, outsider.UserId);
    }
}
