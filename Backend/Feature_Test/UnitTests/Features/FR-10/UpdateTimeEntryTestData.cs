using Application.Commands;
using Application.Handlers;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using TimeEntryReview = Domain.Entities.TimeEntryReview;

namespace UnitTests.Features.FR10;

internal sealed class UpdateTimeEntryWorld
{
    internal User Manager { get; } = CreateUser("manager@example.com", UserRole.Manager);
    internal User Employee { get; } = CreateUser("employee@example.com", UserRole.Employee);
    internal TimeEntry Entry { get; }
    internal UpdateEntryRepository Entries { get; }

    internal UpdateTimeEntryWorld()
    {
        Assert.True(Employee.AssignManager(Manager.UserId, Manager.Role, Manager.Status,
            UserRole.Admin, UserStatus.Active).IsSuccess);
        Entry = TimeEntry.Create(Employee.UserId,
            TaskId.Create(Guid.Parse("6bcb1c4f-c171-4f0a-b17a-e49d3a145dc0")).Value,
            new DateTime(2026, 9, 9), 7.5m, "Original work").Value;
        Entries = new(Entry);
    }

    internal UpdateTimeEntryHandler Handler() => new(Entries);

    internal UpdateTimeEntryCommand Command() => new(
        Entry.Id.Value, Employee.UserId.Value, new DateTime(2026, 9, 10), 8.25m, "  Corrected work  ");

    internal TimeEntryReview ReturnEntry()
    {
        Assert.True(Entry.Return(Manager.UserId, Employee.UserId, Employee.ManagerId,
            Manager.Role, Manager.Status, "Needs correction").IsSuccess);
        return Assert.Single(Entry.Reviews);
    }

    internal EntryState Capture() => new(Entry.Id, Entry.EmployeeId, Entry.TaskId,
        Entry.Date, Entry.Hours, Entry.Comment, Entry.Status, Entry.Reviews.ToArray());

    internal void AssertUnchanged(EntryState before)
    {
        Assert.Equal(before.Id, Entry.Id);
        Assert.Equal(before.EmployeeId, Entry.EmployeeId);
        Assert.Equal(before.TaskId, Entry.TaskId);
        Assert.Equal(before.Date, Entry.Date);
        Assert.Equal(before.Hours, Entry.Hours);
        Assert.Equal(before.Comment, Entry.Comment);
        Assert.Equal(before.Status, Entry.Status);
        Assert.Equal(before.Reviews.Length, Entry.Reviews.Count);
        foreach (var (beforeReview, afterReview) in before.Reviews.Zip(Entry.Reviews))
            Assert.Same(beforeReview, afterReview);
    }

    internal static void AssertFailure(string code, Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }

    private static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;
}

internal sealed record EntryState(TimeEntryId Id, UserId EmployeeId, TaskId TaskId,
    DateTime Date, decimal Hours, string? Comment, TimeEntryStatus Status, TimeEntryReview[] Reviews);

internal sealed class UpdateEntryRepository(TimeEntry entry) : ITimeEntryRepository
{
    internal List<TimeEntryId> RequestedIds { get; } = [];

    public Task<TimeEntry?> GetAsync(TimeEntryId id)
    {
        RequestedIds.Add(id);
        return Task.FromResult<TimeEntry?>(entry.Id == id ? entry : null);
    }

    public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        throw new NotSupportedException();
    public Task<bool> ExistsForTaskAsync(TaskId id) => throw new NotSupportedException();
    public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();
    public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
}
