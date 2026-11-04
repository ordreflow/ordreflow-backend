using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace UnitTests.Features.FR10;

public class TimeEntryEditTests
{
    private static readonly DateTime OriginalDate = new(2026, 9, 9);
    private static readonly DateTime UpdatedDate = new(2026, 9, 10);
    private const decimal OriginalHours = 7.5m;
    private const decimal UpdatedHours = 8.25m;
    private const string OriginalComment = "Completed work";
    private const string UpdatedComment = "Corrected work";

    [Theory]
    [InlineData(nameof(TimeEntry.ChangeHours))]
    [InlineData(nameof(TimeEntry.ChangeDate))]
    [InlineData(nameof(TimeEntry.ChangeComment))]
    public void ReturnedEntry_SuccessfulEdit_ReopensDraftPreservesReturnAndAllowsManagerToReviewAgain(string method)
    {
        var (entry, employee, manager) = CreateAssignedEntry();
        Return(entry, employee, manager);
        var originalReview = Assert.Single(entry.Reviews);
        var originalHistory = Capture(entry).Reviews;
        Assert.Equal(ReviewDecision.Returned, originalReview.Decision);
        Assert.Equal("Needs correction", originalReview.Reason);

        Assert.True(Edit(entry, employee.UserId, method).IsSuccess);

        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        AssertEditedValues(entry, method);
        Assert.Equal<ReviewState>(originalHistory, Capture(entry).Reviews);
        Assert.Same(originalReview, Assert.Single(entry.Reviews));

        Assert.True(entry.Accept(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);
        Assert.Equal(TimeEntryStatus.Accepted, entry.Status);
        Assert.Collection(entry.Reviews,
            review => Assert.Same(originalReview, review),
            review => Assert.Equal(ReviewDecision.Accepted, review.Decision));
    }

    [Theory]
    [InlineData(nameof(TimeEntry.ChangeHours))]
    [InlineData(nameof(TimeEntry.ChangeDate))]
    [InlineData(nameof(TimeEntry.ChangeComment))]
    public void DraftEntry_SuccessfulEdit_RemainsDraftWithoutAddingAReview(string method)
    {
        var (entry, employee, _) = CreateAssignedEntry();

        Assert.True(Edit(entry, employee.UserId, method).IsSuccess);

        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        AssertEditedValues(entry, method);
        Assert.Empty(entry.Reviews);
    }

    [Fact]
    public void Update_ReturnedEntry_ChangesAllFieldsReopensDraftAndPreservesReturnReview()
    {
        var (entry, employee, manager) = CreateAssignedEntry();
        Return(entry, employee, manager);
        var originalReview = Assert.Single(entry.Reviews);
        var originalHistory = Capture(entry).Reviews;

        Assert.True(entry.Update(employee.UserId, UpdatedDate, UpdatedHours, $"  {UpdatedComment}  ").IsSuccess);

        Assert.Equal(UpdatedDate, entry.Date);
        Assert.Equal(UpdatedHours, entry.Hours);
        Assert.Equal(UpdatedComment, entry.Comment);
        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        Assert.Equal<ReviewState>(originalHistory, Capture(entry).Reviews);
        Assert.Same(originalReview, Assert.Single(entry.Reviews));

        Assert.True(entry.Accept(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);
        Assert.Collection(entry.Reviews,
            review => Assert.Same(originalReview, review),
            review => Assert.Equal(ReviewDecision.Accepted, review.Decision));
    }

    [Theory]
    [InlineData(nameof(TimeEntry.Date), "InvalidDate")]
    [InlineData(nameof(TimeEntry.Comment), "CommentTooLong")]
    public void Update_InvalidDateOrComment_DoesNotPartiallyEditReturnedEntry(
        string invalidField, string errorCode)
    {
        var (entry, employee, manager) = CreateAssignedEntry();
        Return(entry, employee, manager);
        var before = Capture(entry);
        var date = invalidField == nameof(TimeEntry.Date) ? default : UpdatedDate;
        var comment = invalidField == nameof(TimeEntry.Comment) ? new string('x', 1001) : UpdatedComment;

        AssertError(errorCode, entry.Update(employee.UserId, date, UpdatedHours, comment));

        AssertUnchanged(before, entry);
    }

    [Theory]
    [InlineData(TimeEntryStatus.Returned, "EntryOwnerMismatch")]
    [InlineData(TimeEntryStatus.Accepted, "TimeEntryNotEditable")]
    public void Update_UnauthorizedOrAcceptedEntry_LeavesFieldsStatusAndReviewsUnchanged(
        TimeEntryStatus initialStatus, string errorCode)
    {
        var (entry, employee, manager) = CreateAssignedEntry();
        if (initialStatus == TimeEntryStatus.Returned)
            Return(entry, employee, manager);
        else
            Assert.True(entry.Accept(manager.UserId, employee.UserId, employee.ManagerId,
                manager.Role, manager.Status).IsSuccess);
        var before = Capture(entry);
        var actorId = initialStatus == TimeEntryStatus.Returned ? manager.UserId : employee.UserId;

        AssertError(errorCode, entry.Update(actorId, UpdatedDate, UpdatedHours, UpdatedComment));

        AssertUnchanged(before, entry);
    }

    [Theory]
    [InlineData(nameof(TimeEntry.ChangeHours), TimeEntryStatus.Draft, "InvalidHours")]
    [InlineData(nameof(TimeEntry.ChangeDate), TimeEntryStatus.Draft, "InvalidDate")]
    [InlineData(nameof(TimeEntry.ChangeComment), TimeEntryStatus.Draft, "CommentTooLong")]
    [InlineData(nameof(TimeEntry.ChangeHours), TimeEntryStatus.Returned, "InvalidHours")]
    [InlineData(nameof(TimeEntry.ChangeDate), TimeEntryStatus.Returned, "InvalidDate")]
    [InlineData(nameof(TimeEntry.ChangeComment), TimeEntryStatus.Returned, "CommentTooLong")]
    public void InvalidEdit_DoesNotChangeFieldsStatusOrReviews(
        string method, TimeEntryStatus initialStatus, string errorCode)
    {
        var (entry, employee, manager) = CreateAssignedEntry();
        if (initialStatus == TimeEntryStatus.Returned)
            Return(entry, employee, manager);
        var before = Capture(entry);

        AssertError(errorCode, Edit(entry, employee.UserId, method, invalid: true));

        AssertUnchanged(before, entry);
    }

    [Theory]
    [InlineData(nameof(TimeEntry.ChangeHours), TimeEntryStatus.Draft)]
    [InlineData(nameof(TimeEntry.ChangeDate), TimeEntryStatus.Draft)]
    [InlineData(nameof(TimeEntry.ChangeComment), TimeEntryStatus.Draft)]
    [InlineData(nameof(TimeEntry.ChangeHours), TimeEntryStatus.Returned)]
    [InlineData(nameof(TimeEntry.ChangeDate), TimeEntryStatus.Returned)]
    [InlineData(nameof(TimeEntry.ChangeComment), TimeEntryStatus.Returned)]
    public void ManagerCannotEditEmployeeEntry_FieldsStatusAndReviewsRemainUnchanged(
        string method, TimeEntryStatus initialStatus)
    {
        var (entry, employee, manager) = CreateAssignedEntry();
        if (initialStatus == TimeEntryStatus.Returned)
            Return(entry, employee, manager);
        var before = Capture(entry);

        AssertError("EntryOwnerMismatch", Edit(entry, manager.UserId, method));

        AssertUnchanged(before, entry);
    }

    [Theory]
    [InlineData(nameof(TimeEntry.ChangeHours), TimeEntryStatus.Accepted)]
    [InlineData(nameof(TimeEntry.ChangeDate), TimeEntryStatus.Accepted)]
    [InlineData(nameof(TimeEntry.ChangeComment), TimeEntryStatus.Accepted)]
    [InlineData(nameof(TimeEntry.ChangeHours), TimeEntryStatus.Finalized)]
    [InlineData(nameof(TimeEntry.ChangeDate), TimeEntryStatus.Finalized)]
    [InlineData(nameof(TimeEntry.ChangeComment), TimeEntryStatus.Finalized)]
    public void AcceptedOrFinalizedEntry_RejectsOwnerEditWithoutChangingFieldsStatusOrReviews(
        string method, TimeEntryStatus initialStatus)
    {
        var (entry, employee, manager) = CreateAssignedEntry();
        Assert.True(entry.Accept(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);
        if (initialStatus == TimeEntryStatus.Finalized)
            Assert.True(entry.Finalize(manager.UserId, employee.UserId, employee.ManagerId,
                manager.Role, manager.Status).IsSuccess);
        var before = Capture(entry);

        AssertError("TimeEntryNotEditable", Edit(entry, employee.UserId, method));

        AssertUnchanged(before, entry);
    }

    private static Result Edit(TimeEntry entry, UserId actorId, string method, bool invalid = false) => method switch
    {
        nameof(TimeEntry.ChangeHours) => entry.ChangeHours(actorId, invalid ? 0m : UpdatedHours),
        nameof(TimeEntry.ChangeDate) => entry.ChangeDate(actorId, invalid ? default : UpdatedDate),
        nameof(TimeEntry.ChangeComment) => entry.ChangeComment(actorId,
            invalid ? new string('x', 1001) : $"  {UpdatedComment}  "),
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
    };

    private static void AssertEditedValues(TimeEntry entry, string method)
    {
        Assert.Equal(method == nameof(TimeEntry.ChangeHours) ? UpdatedHours : OriginalHours, entry.Hours);
        Assert.Equal(method == nameof(TimeEntry.ChangeDate) ? UpdatedDate : OriginalDate, entry.Date);
        Assert.Equal(method == nameof(TimeEntry.ChangeComment) ? UpdatedComment : OriginalComment, entry.Comment);
    }

    private static (TimeEntry Entry, User Employee, User Manager) CreateAssignedEntry()
    {
        var manager = User.Create(PersonName.Create("Test Manager").Value,
            EmailAddress.Create("manager@example.com").Value, UserRole.Manager).Value;
        var employee = User.Create(PersonName.Create("Test Employee").Value,
            EmailAddress.Create("employee@example.com").Value, UserRole.Employee).Value;
        Assert.True(employee.AssignManager(manager.UserId, manager.Role, manager.Status,
            UserRole.Admin, UserStatus.Active).IsSuccess);
        var entry = TimeEntry.Create(employee.UserId,
            TaskId.Create(Guid.Parse("6bcb1c4f-c171-4f0a-b17a-e49d3a145dc0")).Value,
            OriginalDate, OriginalHours, OriginalComment).Value;
        return (entry, employee, manager);
    }

    private static void Return(TimeEntry entry, User employee, User manager) =>
        Assert.True(entry.Return(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status, "  Needs correction  ").IsSuccess);

    private static EntryState Capture(TimeEntry entry) => new(
        entry.Id, entry.EmployeeId, entry.TaskId, entry.Date, entry.Hours, entry.Comment, entry.Status,
        entry.Reviews.Select(review => new ReviewState(review.Id.Value, review.TimeEntryId.Value,
            review.Decision, review.Reason, review.ReviewedAt)).ToArray());

    private static void AssertUnchanged(EntryState before, TimeEntry entry)
    {
        var after = Capture(entry);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.EmployeeId, after.EmployeeId);
        Assert.Equal(before.TaskId, after.TaskId);
        Assert.Equal(before.Date, after.Date);
        Assert.Equal(before.Hours, after.Hours);
        Assert.Equal(before.Comment, after.Comment);
        Assert.Equal(before.Status, after.Status);
        Assert.Equal<ReviewState>(before.Reviews, after.Reviews);
    }

    private static void AssertError(string code, Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }

    private sealed record EntryState(TimeEntryId Id, UserId EmployeeId, TaskId TaskId,
        DateTime Date, decimal Hours, string? Comment, TimeEntryStatus Status, ReviewState[] Reviews);

    private sealed record ReviewState(Guid Id, Guid TimeEntryId, ReviewDecision Decision,
        string? Reason, DateTime ReviewedAt);
}
