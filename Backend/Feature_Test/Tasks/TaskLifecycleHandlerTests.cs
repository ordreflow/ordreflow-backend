using System.Text.Json;
using Application.Commands;
using Application.Dtos;
using Application.Handlers;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using WebAPI.MappingConfigurations;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace UnitTests.Tasks;

public class TaskLifecycleHandlerTests
{
    [Fact]
    public async Task CloseAndReopenHandlers_ChangeTaskOnItsOrder()
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var order = CreateOrder(manager);
        var task = AddTask(order, manager);
        var users = new FakeUserRepository(manager);
        var orders = new FakeOrderRepository(order);
        var tasks = new FakeTaskRepository(task);

        var closed = await new CloseTaskHandler(users, orders, tasks)
            .HandleAsync(new CloseTaskCommand(task.TaskId.Value, manager.UserId.Value));

        Assert.True(closed.IsSuccess);
        Assert.Equal(TaskStatus.Closed, task.Status);
        Assert.NotNull(task.ClosedAt);
        AssertError("TaskAlreadyClosed", await new CloseTaskHandler(users, orders, tasks)
            .HandleAsync(new CloseTaskCommand(task.TaskId.Value, manager.UserId.Value)));

        var reopened = await new ReopenTaskHandler(users, orders, tasks)
            .HandleAsync(new ReopenTaskCommand(task.TaskId.Value, manager.UserId.Value));

        Assert.True(reopened.IsSuccess);
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.ClosedAt);
        AssertError("TaskAlreadyOpen", await new ReopenTaskHandler(users, orders, tasks)
            .HandleAsync(new ReopenTaskCommand(task.TaskId.Value, manager.UserId.Value)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TaskHandlers_RejectInvalidAndMissingIdentifiers(bool close)
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var order = CreateOrder(manager);
        var task = AddTask(order, manager);
        var users = new FakeUserRepository(manager);
        var orders = new FakeOrderRepository(order);
        var tasks = new FakeTaskRepository(task);

        AssertError("UserIdEmpty", await HandleAsync(close, users, orders, tasks,
            task.TaskId.Value, Guid.Empty));
        AssertError("WorkIdEmpty", await HandleAsync(close, users, orders, tasks,
            Guid.Empty, manager.UserId.Value));
        AssertError("UserNotFound", await HandleAsync(close, users, orders, tasks,
            task.TaskId.Value, Guid.NewGuid()));
        AssertError("TaskNotFound", await HandleAsync(close, users, orders, tasks,
            Guid.NewGuid(), manager.UserId.Value));
        AssertError("TaskNotFound", await HandleAsync(close, users, new FakeOrderRepository(), tasks,
            task.TaskId.Value, manager.UserId.Value));

        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.ClosedAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TaskHandlers_EnforceOrderScopeAndClosedOrder(bool close)
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var otherManager = CreateUser("other@example.com", UserRole.Manager);
        var order = CreateOrder(manager);
        var task = AddTask(order, manager);
        var users = new FakeUserRepository(manager, otherManager);
        var orders = new FakeOrderRepository(order);
        var tasks = new FakeTaskRepository(task);
        if (!close)
            Assert.True(order.CloseWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId).IsSuccess);
        var originalStatus = task.Status;
        var originalClosedAt = task.ClosedAt;

        AssertError("CaseManagementForbidden", await HandleAsync(close, users, orders, tasks,
            task.TaskId.Value, otherManager.UserId.Value));
        Assert.Equal(originalStatus, task.Status);
        Assert.Equal(originalClosedAt, task.ClosedAt);

        Assert.True(order.Close(manager.UserId, manager.Role, manager.Status).IsSuccess);
        AssertError("CaseClosed", await HandleAsync(close, users, orders, tasks,
            task.TaskId.Value, manager.UserId.Value));
        Assert.Equal(TaskStatus.Closed, task.Status);
    }

    [Fact]
    public async Task GetTask_ProjectsLifecycleStateIntoDto()
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var order = CreateOrder(manager);
        var task = AddTask(order, manager);
        var handler = new GetTaskHandler(new FakeUserRepository(manager),
            new FakeOrderRepository(order), new FakeTaskRepository(task));
        var query = new GetTaskQuery(manager.UserId.Value, task.TaskId.Value);

        var open = await handler.HandleAsync(query);
        Assert.True(open.IsSuccess);
        Assert.Equal(task.TaskId.Value, open.Value.Id);
        Assert.Equal(order.Id.Value, open.Value.OrderId);
        Assert.Equal("Open", open.Value.Status);
        Assert.Null(open.Value.ClosedAt);

        Assert.True(order.CloseWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId).IsSuccess);
        var closed = await handler.HandleAsync(query);
        Assert.True(closed.IsSuccess);
        Assert.Equal("Closed", closed.Value.Status);
        Assert.Equal(task.ClosedAt, closed.Value.ClosedAt);

        Assert.True(order.ReopenWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId).IsSuccess);
        var reopened = await handler.HandleAsync(query);
        Assert.True(reopened.IsSuccess);
        Assert.Equal("Open", reopened.Value.Status);
        Assert.Null(reopened.Value.ClosedAt);
    }

    [Fact]
    public void TaskDtoToResponse_ExposesStatusAndClosedAtInWebJson()
    {
        var closedAt = new DateTime(2026, 9, 9, 10, 30, 0, DateTimeKind.Utc);
        var dto = new TaskDto(Guid.NewGuid(), Guid.NewGuid(), "Repair", "Repair description",
            "Closed", closedAt);

        var response = new TaskToResponseMapper().Map(dto);
        var json = JsonSerializer.SerializeToElement(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.OrderId, response.OrderId);
        Assert.Equal("Closed", response.Status);
        Assert.Equal(closedAt, response.ClosedAt);
        Assert.Equal("Closed", json.GetProperty("status").GetString());
        Assert.Equal(closedAt, json.GetProperty("closedAt").GetDateTime());

        var reopened = new TaskToResponseMapper().Map(dto with { Status = "Open", ClosedAt = null });
        var reopenedJson = JsonSerializer.SerializeToElement(reopened, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Open", reopenedJson.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, reopenedJson.GetProperty("closedAt").ValueKind);
    }

    private static Task<Result> HandleAsync(bool close, IUserRepository users, IOrderRepository orders,
        ITaskRepository tasks, Guid taskId, Guid actorId) => close
        ? new CloseTaskHandler(users, orders, tasks).HandleAsync(new CloseTaskCommand(taskId, actorId))
        : new ReopenTaskHandler(users, orders, tasks).HandleAsync(new ReopenTaskCommand(taskId, actorId));

    private static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    private static Order CreateOrder(User manager) => Order.Create(manager.UserId, manager.Role,
        manager.Status, OrderName.Create("Repair order").Value).Value;

    private static Domain.Entities.Task AddTask(Order order, User manager) =>
        order.AddWorkItem(manager.UserId, manager.Role, manager.Status, "Repair", "Repair description").Value;

    private static void AssertError(string code, Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
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

    private sealed class FakeOrderRepository(params Order[] orders) : IOrderRepository
    {
        public Task<Order?> GetAsync(OrderId id) =>
            Task.FromResult(orders.SingleOrDefault(order => order.Id == id));

        public Task<IReadOnlyList<Order>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<Order>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(Order entity) => throw new NotSupportedException();
        public Task RemoveAsync(OrderId id) => throw new NotSupportedException();
    }

    private sealed class FakeTaskRepository(params Domain.Entities.Task[] tasks) : ITaskRepository
    {
        public Task<Domain.Entities.Task?> GetAsync(TaskId id) =>
            Task.FromResult(tasks.SingleOrDefault(task => task.TaskId == id));

        public Task<IReadOnlyList<Domain.Entities.Task>> GetByOrderIdAsync(OrderId orderId) =>
            throw new NotSupportedException();
    }
}
