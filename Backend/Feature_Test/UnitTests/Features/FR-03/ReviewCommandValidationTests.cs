using Application.Commands;
using Application.Handlers;
using Core.Tools.OperationResult;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Features.FR03;

public class ReviewCommandValidationTests
{
    // Both handlers validate IDs before loading entities; an invalid entry ID takes precedence.
    [Theory]
    [InlineData(false, true, false, "TimeEntryIdEmpty")]
    [InlineData(false, false, true, "UserIdEmpty")]
    [InlineData(false, true, true, "TimeEntryIdEmpty")]
    [InlineData(true, true, false, "TimeEntryIdEmpty")]
    [InlineData(true, false, true, "UserIdEmpty")]
    [InlineData(true, true, true, "TimeEntryIdEmpty")]
    public async Task InvalidIds_ReturnExactErrorWithoutRepositoryCalls(
        bool finalize, bool invalidEntry, bool invalidReviewer, string errorCode)
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        var before = ApprovalTestData.Capture(entry);
        var entries = new FakeApprovalEntryRepository(entry.Id, entry);
        var users = new FakeApprovalUserRepository(employee, manager);

        var result = await Handle(finalize, entries, users,
            invalidEntry ? Guid.Empty : entry.Id.Value,
            invalidReviewer ? Guid.Empty : manager.UserId.Value);

        ApprovalTestData.AssertError(errorCode, result);
        ApprovalTestData.AssertUnchanged(before, entry);
        Assert.Empty(entries.RequestedIds);
        Assert.Empty(users.RequestedIds);
    }

    // A valid but absent entry fails before either user lookup.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingEntry_ReturnsNotFoundWithoutUserLookups(bool finalize)
    {
        var (_, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        var missingId = TimeEntryId.Create(Guid.Parse("9b03137a-4ed6-42f5-a918-048ad0804bd2")).Value;
        var entries = new FakeApprovalEntryRepository(missingId, null);
        var users = new FakeApprovalUserRepository(employee, manager);

        var result = await Handle(finalize, entries, users, missingId.Value, manager.UserId.Value);

        ApprovalTestData.AssertError("TimeEntryNotFound", result);
        Assert.Equal(missingId, Assert.Single(entries.RequestedIds));
        Assert.Empty(users.RequestedIds);
    }

    // The domain service returns UserNotFound if either expected participant cannot be loaded.
    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task MissingEmployeeOrReviewer_RejectsDecisionWithoutChangingEntry(bool finalize, bool missingEmployee)
    {
        var (entry, employee, manager) = ApprovalTestData.CreateAssignedEntry();
        if (finalize)
            ApprovalTestData.Accept(entry, employee, manager);
        var missingReviewerId = UserId.Create(Guid.Parse("cb6a5b05-d7f8-49aa-9210-35eddf0bed38")).Value;
        var reviewerId = missingEmployee ? manager.UserId : missingReviewerId;
        var before = ApprovalTestData.Capture(entry);
        var entries = new FakeApprovalEntryRepository(entry.Id, entry);
        var users = missingEmployee
            ? new FakeApprovalUserRepository(manager)
            : new FakeApprovalUserRepository(employee);
        users.AllowMissing(missingEmployee ? employee.UserId : reviewerId);

        var result = await Handle(finalize, entries, users, entry.Id.Value, reviewerId.Value);

        ApprovalTestData.AssertError("UserNotFound", result);
        ApprovalTestData.AssertUnchanged(before, entry);
        Assert.Equal(entry.Id, Assert.Single(entries.RequestedIds));
        Assert.Contains(missingEmployee ? employee.UserId : reviewerId, users.RequestedIds);
    }

    private static Task<Result> Handle(bool finalize, FakeApprovalEntryRepository entries,
        FakeApprovalUserRepository users, Guid entryId, Guid reviewerId)
    {
        var service = new TimeEntryReviewDomainService();
        return finalize
            ? new FinalizeTimeEntryHandler(entries, users, service)
                .HandleAsync(new FinalizeTimeEntryCommand(entryId, reviewerId))
            : new AcceptTimeEntryHandler(entries, users, service)
                .HandleAsync(new AcceptTimeEntryCommand(entryId, reviewerId));
    }
}
