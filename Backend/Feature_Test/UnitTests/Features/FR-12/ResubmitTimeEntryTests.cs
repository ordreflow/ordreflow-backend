using Application.Commands;
using Application.Handlers;
using Domain.ValueObjects;

namespace UnitTests.Features.FR12;

public class ResubmitTimeEntryTests
{
    // The owner can resubmit a returned entry; resubmission does not erase its return reason.
    [Fact]
    public async Task Owner_ResubmitsReturnedEntryWithoutAddingOrReplacingReview()
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        ReturnResubmissionTestData.Return(entry, employee, manager);
        var returnedReview = Assert.Single(entry.Reviews);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var handler = new ResubmitTimeEntryHandler(entries);

        var result = await handler.HandleAsync(new ResubmitTimeEntryCommand(entry.Id.Value, employee.UserId.Value));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        Assert.Same(returnedReview, Assert.Single(entry.Reviews));
        Assert.Equal(entry.Id, returnedReview.TimeEntryId);
        Assert.Equal(ReviewDecision.Returned, returnedReview.Decision);
        Assert.Equal("Needs correction", returnedReview.Reason);
        Assert.Equal(entry.Id, Assert.Single(entries.RequestedIds));
    }

    // A reviewer or other non-owner cannot resubmit the employee's returned entry.
    [Fact]
    public async Task NonOwner_CannotResubmitReturnedEntry()
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        ReturnResubmissionTestData.Return(entry, employee, manager);
        var before = ReturnResubmissionTestData.Capture(entry);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var handler = new ResubmitTimeEntryHandler(entries);

        var result = await handler.HandleAsync(new ResubmitTimeEntryCommand(entry.Id.Value, manager.UserId.Value));

        ReturnResubmissionTestData.AssertError("EntryOwnerMismatch", result);
        ReturnResubmissionTestData.AssertUnchanged(before, entry);
        Assert.Equal(entry.Id, Assert.Single(entries.RequestedIds));
    }

    // Only Returned can be resubmitted; accepted and finalized entries fail the editability check first.
    [Theory]
    [InlineData(TimeEntryStatus.Draft, "TimeEntryNotReturned")]
    [InlineData(TimeEntryStatus.Accepted, "TimeEntryNotEditable")]
    [InlineData(TimeEntryStatus.Finalized, "TimeEntryNotEditable")]
    public async Task NonReturnedEntry_RejectsResubmissionWithoutMutation(TimeEntryStatus status, string errorCode)
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        if (status != TimeEntryStatus.Draft)
        {
            ReturnResubmissionTestData.Accept(entry, employee, manager);
            if (status == TimeEntryStatus.Finalized)
                ReturnResubmissionTestData.Finalize(entry, employee, manager);
        }

        Assert.Equal(status, entry.Status);
        var before = ReturnResubmissionTestData.Capture(entry);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var handler = new ResubmitTimeEntryHandler(entries);

        var result = await handler.HandleAsync(new ResubmitTimeEntryCommand(entry.Id.Value, employee.UserId.Value));

        ReturnResubmissionTestData.AssertError(errorCode, result);
        ReturnResubmissionTestData.AssertUnchanged(before, entry);
        Assert.Equal(entry.Id, Assert.Single(entries.RequestedIds));
    }

    // Currently correcting a returned entry reopens Draft, so a subsequent resubmit is rejected.
    [Fact]
    public async Task CorrectedReturnedEntry_IsAlreadyDraftAndCannotBeResubmitted()
    {
        var (entry, employee, manager) = ReturnResubmissionTestData.CreateAssignedEntry();
        ReturnResubmissionTestData.Return(entry, employee, manager);
        Assert.True(entry.Update(employee.UserId, entry.Date, 6m, "Corrected work").IsSuccess);
        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        var before = ReturnResubmissionTestData.Capture(entry);
        var entries = new FakeReturnEntryRepository(entry.Id, entry);
        var handler = new ResubmitTimeEntryHandler(entries);

        var result = await handler.HandleAsync(new ResubmitTimeEntryCommand(entry.Id.Value, employee.UserId.Value));

        ReturnResubmissionTestData.AssertError("TimeEntryNotReturned", result);
        ReturnResubmissionTestData.AssertUnchanged(before, entry);
        Assert.Equal(entry.Id, Assert.Single(entries.RequestedIds));
    }
}
