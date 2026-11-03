using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR02;

internal static class ManagerTimeEntryTestData
{
    internal static User CreateManager() => CreateUser("manager@example.com", UserRole.Manager);

    internal static User CreateEmployee() => CreateUser("employee@example.com", UserRole.Employee);

    internal static User CreateAssignedEmployee(User manager, string email = "employee@example.com")
    {
        var employee = CreateUser(email, UserRole.Employee);
        if (employee.AssignManager(manager.UserId, manager.Role, manager.Status,
                UserRole.Admin, UserStatus.Active).IsFailure)
            throw new InvalidOperationException("Could not assign the test employee to the manager.");

        return employee;
    }

    internal static TimeEntry CreateEntry(User employee, DateTime date, decimal hours = 7.5m,
        string? comment = null) => TimeEntry.Create(
        employee.UserId, TaskId.Create(Guid.NewGuid()).Value, date, hours, comment).Value;

    private static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;
}

internal sealed class FakeUserRepository(params User[] users) : IUserRepository
{
    private readonly List<UserId> requestedIds = [];

    internal IReadOnlyList<UserId> RequestedIds => requestedIds;

    public Task<User?> GetAsync(UserId id)
    {
        requestedIds.Add(id);
        return Task.FromResult(users.SingleOrDefault(user => user.UserId == id));
    }

    public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
    public Task AddAsync(User entity) => throw new NotSupportedException();
    public Task RemoveAsync(UserId id) => throw new NotSupportedException();
}

internal sealed class FakeTimeEntryRepository(params TimeEntry[] preScopedEntries) : ITimeEntryRepository
{
    private readonly List<UserId> requestedIds = [];

    internal IReadOnlyList<UserId> RequestedIds => requestedIds;

    public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId managerId)
    {
        requestedIds.Add(managerId);
        // The caller supplies pre-scoped entries; only the real repository filters by employee.ManagerId.
        return Task.FromResult<IReadOnlyList<TimeEntry>>(preScopedEntries);
    }

    public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
    public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId employeeId) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) => throw new NotSupportedException();
    public Task<bool> ExistsForTaskAsync(TaskId taskId) => throw new NotSupportedException();
    public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();
    public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
}
