using Application.Commands;
using Application.Handlers;
using Core.Tools.OperationResult;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Features.FR12;

public class ReturnAndResubmitValidationTests
{
    // Both handlers validate IDs before repository access; an empty entry ID takes precedence.
    [Theory]
    [InlineData(false, true, false, "TimeEntryIdEmpty")]
    [InlineData(false, false, true, "UserIdEmpty")]
    [InlineData(false, true, true, "TimeEntryIdEmpty")]
    [InlineData(true, true, false, "TimeEntryIdEmpty")]
    [InlineData(true, false, true, "UserIdEmpty")]
    [InlineData(true, true, true, "TimeEntryIdEmpty")]
    public async Task InvalidIds_RejectWithoutFetchingEntryOrUsers(
        bool resubmit, bool invalidEntry, bool invalidActor, string errorCode)
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        var before = ReturnResubmissionTestData.Capture(entry);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var users = new FakeReturnUserRepository(employee, manager);

        var result = await Handle(resubmit, entries, users,
            invalidEntry ? Guid.Empty : entry.Id.Value,
            invalidActor ? Guid.Empty : (resubmit ? employee.UserId.Value : manager.UserId.Value));

        ReturnResubmissionTestData.AssertError(errorCode, result);
        ReturnResubmissionTestData.AssertUnchanged(before, entry);
        Assert.Empty(entries.RequestedIds);
        Assert.Empty(users.RequestedIds);
    }

    // An absent entry is reported before fetching participants or changing any state.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingEntry_ReturnsNotFoundWithoutUserLookups(bool resubmit)
    {
        var (_, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        var missingId = TimeEntryId.Create(Guid.Parse("9b03137a-4ed6-42f5-a918-048ad0804bd2")).Value;
        var entries = new FakeReturnEntryRepository(missingId, null);
        var users = new FakeReturnUserRepository(employee, manager);

        var result = await Handle(resubmit, entries, users, missingId.Value,
            resubmit ? employee.UserId.Value : manager.UserId.Value);

        ReturnResubmissionTestData.AssertError("TimeEntryNotFound", result);
        Assert.Equal(missingId, Assert.Single(entries.RequestedIds));
        Assert.Empty(users.RequestedIds);
    }

    // Returning an entry requires the employee and reviewer records to both exist.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingEmployeeOrReviewer_RejectsReturnWithoutMutation(bool missingEmployee)
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        var missingReviewerId = UserId.Create(Guid.Parse("cb6a5b05-d7f8-49aa-9210-35eddf0bed38")).Value;
        var reviewerId = missingEmployee ? manager.UserId : missingReviewerId;
        var before = ReturnResubmissionTestData.Capture(entry);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var users = missingEmployee
            ? new FakeReturnUserRepository(manager)
            : new FakeReturnUserRepository(employee);
        users.AllowMissing(missingEmployee ? employee.UserId : reviewerId);
        var handler = new ReturnTimeEntryHandler(entries, users, new TimeEntryReviewDomainService());

        var result = await handler.HandleAsync(new ReturnTimeEntryCommand(
            entry.Id.Value, reviewerId.Value, "Needs correction"));

        ReturnResubmissionTestData.AssertError("UserNotFound", result);
        ReturnResubmissionTestData.AssertUnchanged(before, entry);
        ReturnResubmissionTestData.AssertReviewLookups(entries, users, entry.Id, employee.UserId, reviewerId);
    }

    private static Task<Result> Handle(bool resubmit, FakeReturnEntryRepository entries,
        FakeReturnUserRepository users, Guid entryId, Guid actorId) => resubmit
        ? new ResubmitTimeEntryHandler(entries).HandleAsync(new ResubmitTimeEntryCommand(entryId, actorId))
        : new ReturnTimeEntryHandler(entries, users, new TimeEntryReviewDomainService())
            .HandleAsync(new ReturnTimeEntryCommand(entryId, actorId, "Needs correction"));
}
