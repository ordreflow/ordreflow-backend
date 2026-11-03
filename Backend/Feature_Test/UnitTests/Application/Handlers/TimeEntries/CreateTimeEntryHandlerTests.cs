using Application.Commands;
using Application.Handlers;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Application.Handlers.TimeEntries;

public class CreateTimeEntryHandlerTests
{
    private static readonly DateTime EntryDate = new(2026, 10, 5);

    [Theory]
    [InlineData(true, "UserIdEmpty")]
    [InlineData(false, "WorkIdEmpty")]
    public async Task EmptyIdentifier_IsRejectedBeforeRepositoryAccess(bool emptyEmployeeId, string errorCode)
    {
        var world = new World();
        var command = emptyEmployeeId
            ? world.Command() with { EmployeeId = Guid.Empty }
            : world.Command() with { TaskId = Guid.Empty };

        AssertFailure(errorCode, await world.Handler().HandleAsync(command), world);

        Assert.Empty(world.Users.RequestedIds);
        Assert.Empty(world.Tasks.RequestedIds);
        Assert.Empty(world.Orders.RequestedIds);
    }

    [Fact]
    public async Task MissingEmployee_DoesNotLookUpTaskOrSave()
    {
        var world = new World();
        world.Users.User = null;

        AssertFailure("EmployeeNotFound", await world.Handler().HandleAsync(world.Command()), world);

        Assert.Equal(world.Employee.UserId, Assert.Single(world.Users.RequestedIds));
        Assert.Empty(world.Tasks.RequestedIds);
        Assert.Empty(world.Orders.RequestedIds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrDetachedTask_DoesNotLookUpOrderOrSave(bool detached)
    {
        var world = new World();
        world.Tasks.WorkItem = detached
            ? Domain.Entities.Task.Create("Detached", "Not assigned to an order").Value
            : null;
        var command = detached
            ? world.Command() with { TaskId = world.Tasks.WorkItem!.TaskId.Value }
            : world.Command();

        AssertFailure("TaskNotFound", await world.Handler().HandleAsync(command), world);

        Assert.Equal(TaskId.Create(command.TaskId).Value, Assert.Single(world.Tasks.RequestedIds));
        Assert.Empty(world.Orders.RequestedIds);
    }

    [Fact]
    public async Task MissingOrder_DoesNotSave()
    {
        var world = new World();
        world.Orders.Order = null;

        AssertFailure("OrderNotFound", await world.Handler().HandleAsync(world.Command()), world);

        Assert.Equal(world.Order.Id, Assert.Single(world.Orders.RequestedIds));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(25)]
    public async Task InvalidHours_AreRejectedWithoutSaving(int hours)
    {
        var world = new World();

        AssertFailure("InvalidHours", await world.Handler().HandleAsync(
            world.Command() with { Hours = hours }), world);
    }

    [Fact]
    public async Task FractionalHoursAboveMaximum_AreRejectedWithoutSaving()
    {
        var world = new World();

        AssertFailure("InvalidHours", await world.Handler().HandleAsync(
            world.Command() with { Hours = 24.5m }), world);
    }

    [Fact]
    public async Task MaximumHours_AreAccepted()
    {
        var world = new World();
        world.Entries.AllowAdd = true;

        var result = await world.Handler().HandleAsync(world.Command() with { Hours = 24m });

        Assert.True(result.IsSuccess);
        Assert.Equal(24m, Assert.Single(world.Entries.Added).Hours);
    }

    [Fact]
    public async Task MissingDate_IsRejectedWithoutSaving()
    {
        var world = new World();

        AssertFailure("InvalidDate", await world.Handler().HandleAsync(
            world.Command() with { Date = default }), world);
    }

    [Fact]
    public async Task ExcessivelyLongComment_IsRejectedWithoutSaving()
    {
        var world = new World();

        AssertFailure("CommentTooLong", await world.Handler().HandleAsync(
            world.Command() with { Comment = new string('x', 1001) }), world);
    }

    [Fact]
    public async Task InactiveEmployee_CannotRegisterTime()
    {
        var world = new World();
        Assert.True(world.Employee.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        AssertFailure("UserInactive", await world.Handler().HandleAsync(world.Command()), world);
    }

    [Fact]
    public async Task EmployeeOutsideOrderManagersScope_CannotRegisterTime()
    {
        var world = new World();
        var otherManager = CreateUser("other-manager@example.com", UserRole.Manager);
        Assert.True(world.Employee.AssignManager(otherManager.UserId, otherManager.Role,
            otherManager.Status, UserRole.Admin, UserStatus.Active).IsSuccess);

        AssertFailure("ManagerScopeForbidden", await world.Handler().HandleAsync(world.Command()), world);
    }

    [Theory]
    [InlineData(true, "CaseClosed")]
    [InlineData(false, "TaskClosed")]
    public async Task ClosedOrderOrTask_CannotReceiveTimeEntries(bool closeOrder, string errorCode)
    {
        var world = new World();
        var closed = closeOrder
            ? world.Order.Close(world.Manager.UserId, world.Manager.Role, world.Manager.Status)
            : world.Order.CloseWorkItem(world.Manager.UserId, world.Manager.Role, world.Manager.Status,
                world.WorkItem.TaskId);
        Assert.True(closed.IsSuccess);

        AssertFailure(errorCode, await world.Handler().HandleAsync(world.Command()), world);
    }

    [Fact]
    public async Task ValidRegistration_SavesOneDraftWithCommandValuesAndLooksUpExpectedEntities()
    {
        var world = new World();
        world.Entries.AllowAdd = true;
        var command = world.Command();

        var result = await world.Handler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(world.Employee.UserId, Assert.Single(world.Users.RequestedIds));
        Assert.Equal(world.WorkItem.TaskId, Assert.Single(world.Tasks.RequestedIds));
        Assert.Equal(world.Order.Id, Assert.Single(world.Orders.RequestedIds));
        var saved = Assert.Single(world.Entries.Added);
        Assert.Equal(command.EmployeeId, saved.EmployeeId.Value);
        Assert.Equal(command.TaskId, saved.TaskId.Value);
        Assert.Equal(command.Date, saved.Date);
        Assert.Equal(command.Hours, saved.Hours);
        Assert.Equal("Finished work", saved.Comment); // TimeEntry.Create trims the comment.
        Assert.Equal(TimeEntryStatus.Draft, saved.Status);
    }

    private static void AssertFailure(string code, Result result, World world)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
        Assert.Empty(world.Entries.Added);
    }

    private static User CreateUser(string email, UserRole role = UserRole.Employee) =>
        User.Create(PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    private sealed class World
    {
        public User Manager { get; } = CreateUser("manager@example.com", UserRole.Manager);
        public User Employee { get; } = CreateUser("employee@example.com");
        public Order Order { get; }
        public Domain.Entities.Task WorkItem { get; }
        public FakeUserRepository Users { get; }
        public FakeTaskRepository Tasks { get; }
        public FakeOrderRepository Orders { get; }
        public FakeTimeEntryRepository Entries { get; } = new();

        public World()
        {
            Assert.True(Employee.AssignManager(Manager.UserId, Manager.Role, Manager.Status,
                UserRole.Admin, UserStatus.Active).IsSuccess);
            Order = Domain.Aggregate.Order.Create(Manager.UserId, Manager.Role, Manager.Status,
                OrderName.Create("Repair order").Value).Value;
            WorkItem = Order.AddWorkItem(Manager.UserId, Manager.Role, Manager.Status,
                "Repair", "Repair the device").Value;
            Users = new FakeUserRepository(Employee);
            Tasks = new FakeTaskRepository(WorkItem);
            Orders = new FakeOrderRepository(Order);
        }

        public CreateTimeEntryCommand Command() =>
            new(Employee.UserId.Value, WorkItem.TaskId.Value, EntryDate, 7.5m, "  Finished work  ");

        public CreateTimeEntryHandler Handler() =>
            new(Entries, Users, Tasks, Orders, new TimeRegistrationDomainService());
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public User? User { get; set; } = user;
        public List<UserId> RequestedIds { get; } = [];

        public Task<User?> GetAsync(UserId id)
        {
            RequestedIds.Add(id);
            return Task.FromResult(User?.UserId == id ? User : null);
        }

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(User entity) => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }

    private sealed class FakeTaskRepository(Domain.Entities.Task? workItem) : ITaskRepository
    {
        public Domain.Entities.Task? WorkItem { get; set; } = workItem;
        public List<TaskId> RequestedIds { get; } = [];

        public Task<Domain.Entities.Task?> GetAsync(TaskId id)
        {
            RequestedIds.Add(id);
            return Task.FromResult(WorkItem?.TaskId == id ? WorkItem : null);
        }

        public Task<IReadOnlyList<Domain.Entities.Task>> GetByOrderIdAsync(OrderId orderId) =>
            throw new NotSupportedException();
    }

    private sealed class FakeOrderRepository(Order? order) : IOrderRepository
    {
        public Order? Order { get; set; } = order;
        public List<OrderId> RequestedIds { get; } = [];

        public Task<Order?> GetAsync(OrderId id)
        {
            RequestedIds.Add(id);
            return Task.FromResult(Order?.Id == id ? Order : null);
        }

        public Task<IReadOnlyList<Order>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<Order>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(Order entity) => throw new NotSupportedException();
        public Task RemoveAsync(OrderId id) => throw new NotSupportedException();
    }

    private sealed class FakeTimeEntryRepository : ITimeEntryRepository
    {
        public bool AllowAdd { get; set; }
        public List<TimeEntry> Added { get; } = [];

        public Task AddAsync(TimeEntry entity)
        {
            if (!AllowAdd)
                throw new InvalidOperationException("Unexpected time entry persistence.");

            Added.Add(entity);
            return Task.CompletedTask;
        }

        public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId employeeId) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) => throw new NotSupportedException();
        public Task<bool> ExistsForTaskAsync(TaskId taskId) => throw new NotSupportedException();
        public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
    }
}
