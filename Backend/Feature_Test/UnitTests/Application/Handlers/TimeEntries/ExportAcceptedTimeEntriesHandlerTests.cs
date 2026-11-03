using Application.Handlers;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Application.Handlers.TimeEntries;

public class ExportAcceptedTimeEntriesHandlerTests
{
    private static readonly DateTime From = new(2026, 10, 1);
    private static readonly DateTime ToExclusive = new(2026, 11, 1);

    [Fact]
    public async Task Manager_ExportsOnlyOwnEmployeesAcceptedAndFinalizedEntriesWithNames()
    {
        var world = new World();
        var accepted = world.Entry(world.Anna, new DateTime(2026, 10, 5), 7.5m, accept: true);
        var finalized = world.Entry(world.Anna, new DateTime(2026, 10, 6), 2m, accept: true, finalize: true);
        world.Entry(world.Anna, new DateTime(2026, 10, 7), 3m);                       // draft
        world.Entry(world.Anna, new DateTime(2026, 10, 8), 4m, returnIt: true);       // returned
        world.Entry(world.David, new DateTime(2026, 10, 5), 8m, accept: true);        // other manager's employee

        var result = await world.Export(world.Mette);

        Assert.True(result.IsSuccess);
        Assert.Equal([accepted.Id.Value, finalized.Id.Value], result.Value.Select(row => row.TimeEntryId));
        var row = result.Value[0];
        Assert.Equal("Anna Andersen", row.EmployeeName);
        Assert.Equal("Opsæt reoler", row.TaskTitle);
        Assert.Equal("Netto Vestergade", row.OrderName);
        Assert.Equal("Accepted", row.Status);
        Assert.Equal("Finalized", result.Value[1].Status);
    }

    [Fact]
    public async Task Admin_ExportsAcceptedEntriesForAllEmployees()
    {
        var world = new World();
        world.Entry(world.Anna, new DateTime(2026, 10, 5), 7.5m, accept: true);
        world.Entry(world.David, new DateTime(2026, 10, 5), 8m, accept: true);
        world.Entry(world.David, new DateTime(2026, 10, 6), 8m);

        var result = await world.Export(world.Admin);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Anna Andersen", "David Dam"], result.Value.Select(row => row.EmployeeName));
    }

    [Fact]
    public async Task EmployeeFilter_ExportsOnlyThatEmployee()
    {
        var world = new World();
        var annaEntry = world.Entry(world.Anna, new DateTime(2026, 10, 5), 7.5m, accept: true);
        world.Entry(world.David, new DateTime(2026, 10, 5), 8m, accept: true);

        var result = await world.Export(world.Admin, world.Anna);

        Assert.Equal(annaEntry.Id.Value, Assert.Single(result.Value).TimeEntryId);
    }

    [Fact]
    public async Task Manager_FilteringOnAnotherManagersEmployee_GetsNothing()
    {
        var world = new World();
        world.Entry(world.David, new DateTime(2026, 10, 5), 8m, accept: true);

        var result = await world.Export(world.Mette, world.David);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task EntriesOutsideThePeriod_AreNotExported()
    {
        var world = new World();
        world.Entry(world.Anna, new DateTime(2026, 9, 30), 1m, accept: true);
        var inside = world.Entry(world.Anna, new DateTime(2026, 10, 31), 2m, accept: true);
        world.Entry(world.Anna, new DateTime(2026, 11, 1), 3m, accept: true);

        var result = await world.Export(world.Mette);

        Assert.Equal(inside.Id.Value, Assert.Single(result.Value).TimeEntryId);
    }

    [Fact]
    public async Task Employee_CannotExport()
    {
        var world = new World();

        var result = await world.Export(world.Anna);

        AssertError("ExportForbidden", result);
    }

    [Fact]
    public async Task EndBeforeStart_IsRejected()
    {
        var world = new World();

        var result = await world.CreateHandler()
            .HandleAsync(new ExportAcceptedTimeEntriesQuery(world.Mette.UserId.Value, ToExclusive, From));

        AssertError("InvalidDateRange", result);
    }

    private static void AssertError(string code, Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }

    // Two managers with one employee each, an admin, and one order with one task.
    private sealed class World
    {
        public User Admin { get; } = CreateUser("Admin", "admin@example.com", UserRole.Admin);
        public User Mette { get; } = CreateUser("Mette Madsen", "mette@example.com", UserRole.Manager);
        public User Lars { get; } = CreateUser("Lars Larsen", "lars@example.com", UserRole.Manager);
        public User Anna { get; } = CreateUser("Anna Andersen", "anna@example.com");
        public User David { get; } = CreateUser("David Dam", "david@example.com");

        private readonly Order order;
        private readonly Domain.Entities.Task task;
        private readonly List<TimeEntry> entries = [];

        public World()
        {
            Assert.True(Anna.AssignManager(Mette.UserId, Mette.Role, Mette.Status, Admin.Role, Admin.Status).IsSuccess);
            Assert.True(David.AssignManager(Lars.UserId, Lars.Role, Lars.Status, Admin.Role, Admin.Status).IsSuccess);
            order = Order.Create(Mette.UserId, Mette.Role, Mette.Status, OrderName.Create("Netto Vestergade").Value).Value;
            task = order.AddWorkItem(Mette.UserId, Mette.Role, Mette.Status, "Opsæt reoler", "Reoler").Value;
        }

        public TimeEntry Entry(User employee, DateTime date, decimal hours,
            bool accept = false, bool finalize = false, bool returnIt = false)
        {
            var entry = TimeEntry.Create(employee.UserId, task.TaskId, date, hours, null).Value;
            var manager = employee == Anna ? Mette : Lars;
            if (accept)
                Assert.True(entry.Accept(manager.UserId, employee.UserId, employee.ManagerId, manager.Role, manager.Status).IsSuccess);
            if (finalize)
                Assert.True(entry.Finalize(manager.UserId, employee.UserId, employee.ManagerId, manager.Role, manager.Status).IsSuccess);
            if (returnIt)
                Assert.True(entry.Return(manager.UserId, employee.UserId, employee.ManagerId, manager.Role, manager.Status, "Fix").IsSuccess);
            entries.Add(entry);
            return entry;
        }

        public Task<Result<IReadOnlyList<TimeEntryExportRow>>> Export(User requester, User? employee = null) =>
            CreateHandler().HandleAsync(new ExportAcceptedTimeEntriesQuery(
                requester.UserId.Value, From, ToExclusive, employee?.UserId.Value));

        public ExportAcceptedTimeEntriesHandler CreateHandler() => new(
            new FakeTimeEntryRepository(entries),
            new FakeUserRepository(Admin, Mette, Lars, Anna, David),
            new FakeTaskRepository(task),
            new FakeOrderRepository(order));

        private static User CreateUser(string name, string email, UserRole role = UserRole.Employee) =>
            User.Create(PersonName.Create(name).Value, EmailAddress.Create(email).Value, role).Value;
    }

    private sealed class FakeTimeEntryRepository(List<TimeEntry> entries) : ITimeEntryRepository
    {
        public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) =>
            Task.FromResult<IReadOnlyList<TimeEntry>>(
                entries.Where(entry => entry.Date >= from && entry.Date < to).ToArray());

        public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId employeeId) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<bool> ExistsForTaskAsync(TaskId taskId) => throw new NotSupportedException();
        public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();
        public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetAsync(UserId id) =>
            Task.FromResult(users.SingleOrDefault(user => user.UserId == id));

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(User entity) => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }

    private sealed class FakeTaskRepository(params Domain.Entities.Task[] tasks) : ITaskRepository
    {
        public Task<Domain.Entities.Task?> GetAsync(TaskId id) =>
            Task.FromResult(tasks.SingleOrDefault(task => task.TaskId == id));

        public Task<IReadOnlyList<Domain.Entities.Task>> GetByOrderIdAsync(OrderId orderId) =>
            throw new NotSupportedException();
    }

    private sealed class FakeOrderRepository(params Order[] orders) : IOrderRepository
    {
        public Task<Order?> GetAsync(OrderId id) =>
            Task.FromResult(orders.SingleOrDefault(order => order.Id == id));

        public Task<IReadOnlyList<Order>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<Order>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(Order entity) => throw new NotSupportedException();
        public Task RemoveAsync(OrderId id) => throw new NotSupportedException();
    }
}
