using Domain.Aggregate;
using Domain.ValueObjects;

namespace UnitTests.Features.FR01;

public class TimeEntryCreationBoundariesTests
{
    private static readonly DateTime EntryDate = new(2026, 9, 9);

    // A small positive duration and an omitted comment create a draft with no reviews.
    [Fact]
    public void Create_AcceptsSmallPositiveDecimalHoursAndNullComment_AsDraftWithoutReviews()
    {
        var (_, employee, _, task) = TimeRegistrationTestData.CreateWork();

        var result = TimeEntry.Create(employee.UserId, task.TaskId, EntryDate, 0.01m, null);

        Assert.True(result.IsSuccess);
        var entry = result.Value;
        Assert.Equal(employee.UserId, entry.EmployeeId);
        Assert.Equal(task.TaskId, entry.TaskId);
        Assert.Equal(EntryDate, entry.Date);
        Assert.Equal(0.01m, entry.Hours);
        Assert.Null(entry.Comment);
        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        Assert.Empty(entry.Reviews);
    }

    // Trimming occurs before the 1,000-character comment limit is checked.
    [Fact]
    public void Create_AcceptsCommentExactlyAtMaximumLengthAfterTrimming()
    {
        var (_, employee, _, task) = TimeRegistrationTestData.CreateWork();
        var comment = new string('x', 1000);

        var result = TimeEntry.Create(employee.UserId, task.TaskId, EntryDate, 2m, $" \t{comment}\n ");

        Assert.True(result.IsSuccess);
        Assert.Equal(comment, result.Value.Comment);
        Assert.Equal(1000, result.Value.Comment!.Length);
    }

    // Creating a time entry fails when no employee ID is provided.
    [Fact]
    public void Create_RejectsNullEmployeeId()
    {
        var (_, _, _, task) = TimeRegistrationTestData.CreateWork();

        // Deliberately bypass the non-nullable signature to test the runtime boundary.
        var result = TimeEntry.Create(null!, task.TaskId, EntryDate, 2m, null);

        Assert.True(result.IsFailure);
        Assert.Equal("EmployeeRequired", Assert.Single(result.Errors).Code);
    }

    // Creating a time entry fails when no task ID is provided.
    [Fact]
    public void Create_RejectsNullTaskId()
    {
        var (_, employee, _, _) = TimeRegistrationTestData.CreateWork();

        // Deliberately bypass the non-nullable signature to test the runtime boundary.
        var result = TimeEntry.Create(employee.UserId, null!, EntryDate, 2m, null);

        Assert.True(result.IsFailure);
        Assert.Equal("WorkItemRequired", Assert.Single(result.Errors).Code);
    }
}
