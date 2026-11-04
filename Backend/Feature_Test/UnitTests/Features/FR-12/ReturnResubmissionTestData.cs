using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR12;

internal static class ReturnResubmissionTestData
{
    internal static (TimeEntry Entry, User Employee, User Manager) CreateAssignedEntry()
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var employee = CreateUser("employee@example.com", UserRole.Employee);
        Assert.True(employee.AssignManager(manager.UserId, manager.Role, manager.Status,
            UserRole.Admin, UserStatus.Active).IsSuccess);

        var entry = TimeEntry.Create(employee.UserId,
            TaskId.Create(Guid.Parse("6bcb1c4f-c171-4f0a-b17a-e49d3a145dc0")).Value,
            new DateTime(2026, 9, 9), 7.5m, "Completed work").Value;
        return (entry, employee, manager);
    }

    internal static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    internal static void Return(TimeEntry entry, User employee, User manager) =>
        Assert.True(entry.Return(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status, "  Needs correction  ").IsSuccess);

    internal static void Accept(TimeEntry entry, User employee, User manager) =>
        Assert.True(entry.Accept(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);

    internal static void Finalize(TimeEntry entry, User employee, User manager) =>
        Assert.True(entry.Finalize(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);

    internal static void AssertError(string code, Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }

    internal static EntryState Capture(TimeEntry entry) => new(
        entry.Id, entry.EmployeeId, entry.TaskId, entry.Date, entry.Hours, entry.Comment, entry.Status,
        entry.Reviews.Select(review => new ReviewState(review.Id.Value, review.TimeEntryId,
            review.Decision, review.Reason, review.ReviewedAt)).ToArray());

    internal static void AssertUnchanged(EntryState before, TimeEntry entry)
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

    internal static void AssertReviewLookups(FakeReturnEntryRepository entries, FakeReturnUserRepository users,
        TimeEntryId entryId, UserId employeeId, UserId reviewerId)
    {
        Assert.Equal(entryId, Assert.Single(entries.RequestedIds));
        Assert.Equal<UserId>(new[] { employeeId, reviewerId }, users.RequestedIds);
    }

    internal sealed record EntryState(TimeEntryId Id, UserId EmployeeId, TaskId TaskId,
        DateTime Date, decimal Hours, string? Comment, TimeEntryStatus Status, ReviewState[] Reviews);

    internal sealed record ReviewState(Guid Id, TimeEntryId TimeEntryId, ReviewDecision Decision,
        string? Reason, DateTime ReviewedAt);
}

internal sealed class FakeReturnEntryRepository(TimeEntryId expectedId, TimeEntry? entry) : ITimeEntryRepository
{
    private readonly List<TimeEntryId> _requestedIds = [];
    internal IReadOnlyList<TimeEntryId> RequestedIds => _requestedIds;

    public Task<TimeEntry?> GetAsync(TimeEntryId id)
    {
        if (id != expectedId || _requestedIds.Count != 0)
            throw new InvalidOperationException("Unexpected time entry lookup.");

        _requestedIds.Add(id);
        return Task.FromResult(entry);
    }

    public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId employeeId) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) => throw new NotSupportedException();
    public Task<bool> ExistsForTaskAsync(TaskId taskId) => throw new NotSupportedException();
    public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();
    public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
}

internal sealed class FakeReturnUserRepository : IUserRepository
{
    private readonly Dictionary<UserId, User?> _users;
    private readonly List<UserId> _requestedIds = [];
    internal IReadOnlyList<UserId> RequestedIds => _requestedIds;

    internal FakeReturnUserRepository(params User[] users) =>
        _users = users.ToDictionary(user => user.UserId, user => (User?)user);

    internal void AllowMissing(UserId id) => _users.Add(id, null);

    public Task<User?> GetAsync(UserId id)
    {
        if (_requestedIds.Count >= 2 || !_users.TryGetValue(id, out var user))
            throw new InvalidOperationException("Unexpected user lookup.");

        _requestedIds.Add(id);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
    public Task AddAsync(User entity) => throw new NotSupportedException();
    public Task RemoveAsync(UserId id) => throw new NotSupportedException();
}
