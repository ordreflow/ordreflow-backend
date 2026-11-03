using Application.Handlers;
using Application.Queries;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR07;

internal sealed class ExportWorld
{
    internal static readonly DateTime From = new(2026, 10, 1);
    internal static readonly DateTime ToExclusive = new(2026, 11, 1);

    internal User Manager { get; } = CreateUser("manager@example.com", UserRole.Manager);
    internal User Admin { get; } = CreateUser("admin@example.com", UserRole.Admin);
    internal User Employee { get; } = CreateUser("employee@example.com", UserRole.Employee);
    internal ExportEntryRepository Entries { get; } = new();
    internal ExportUserRepository Users { get; }
    internal ExportTaskRepository Tasks { get; } = new();
    internal ExportOrderRepository Orders { get; } = new();

    internal ExportWorld()
    {
        Assert.True(Employee.AssignManager(Manager.UserId, Manager.Role, Manager.Status,
            Admin.Role, Admin.Status).IsSuccess);
        Users = new(Manager, Admin, Employee);
    }

    internal ExportAcceptedTimeEntriesQuery Query(Guid? requesterId = null) =>
        new(requesterId ?? Manager.UserId.Value, From, ToExclusive);

    internal ExportAcceptedTimeEntriesHandler Handler() => new(Entries, Users, Tasks, Orders);

    internal TimeEntry CreateAcceptedEntry()
    {
        var taskId = TaskId.Create(Guid.Parse("9adbf6ca-e08f-44b2-a20a-218093671ab8")).Value;
        var entry = TimeEntry.Create(Employee.UserId, taskId, new DateTime(2026, 10, 5), 7.5m, null).Value;
        Assert.True(entry.Accept(Manager.UserId, Employee.UserId, Employee.ManagerId,
            Manager.Role, Manager.Status).IsSuccess);
        Entries.Stored.Add(entry);
        return entry;
    }

    private static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;
}

internal sealed class ExportEntryRepository : ITimeEntryRepository
{
    internal List<TimeEntry> Stored { get; } = [];
    internal List<(DateTime From, DateTime ToExclusive)> Ranges { get; } = [];

    public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        Ranges.Add((from, to));
        return Task.FromResult<IReadOnlyList<TimeEntry>>(
            Stored.Where(entry => entry.Date >= from && entry.Date < to).ToArray());
    }

    public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
    public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId id) => throw new NotSupportedException();
    public Task<bool> ExistsForTaskAsync(TaskId id) => throw new NotSupportedException();
    public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();
    public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
}

internal sealed class ExportUserRepository(params User[] users) : IUserRepository
{
    internal List<User> Stored { get; } = [.. users];
    internal List<UserId> RequestedIds { get; } = [];

    public Task<User?> GetAsync(UserId id)
    {
        RequestedIds.Add(id);
        return Task.FromResult(Stored.SingleOrDefault(user => user.UserId == id));
    }

    public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
    public Task AddAsync(User entity) => throw new NotSupportedException();
    public Task RemoveAsync(UserId id) => throw new NotSupportedException();
}

internal sealed class ExportTaskRepository : ITaskRepository
{
    internal List<TaskId> RequestedIds { get; } = [];

    public Task<Domain.Entities.Task?> GetAsync(TaskId id)
    {
        RequestedIds.Add(id);
        return Task.FromResult<Domain.Entities.Task?>(null);
    }

    public Task<IReadOnlyList<Domain.Entities.Task>> GetByOrderIdAsync(OrderId id) =>
        throw new NotSupportedException();
}

internal sealed class ExportOrderRepository : IOrderRepository
{
    internal List<OrderId> RequestedIds { get; } = [];

    public Task<Order?> GetAsync(OrderId id)
    {
        RequestedIds.Add(id);
        return Task.FromResult<Order?>(null);
    }

    public Task<IReadOnlyList<Order>> GetByManagerIdAsync(UserId id) => throw new NotSupportedException();
    public Task<IReadOnlyList<Order>> GetAllAsync() => throw new NotSupportedException();
    public Task AddAsync(Order entity) => throw new NotSupportedException();
    public Task RemoveAsync(OrderId id) => throw new NotSupportedException();
}
