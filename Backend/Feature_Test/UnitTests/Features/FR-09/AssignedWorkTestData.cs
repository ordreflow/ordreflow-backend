using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using TaskEntity = Domain.Entities.Task;

namespace UnitTests.Features.FR09;

internal static class AssignedWorkTestData
{
    internal static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    internal static User CreateAssignedEmployee(User manager)
    {
        var employee = CreateUser("employee@example.com", UserRole.Employee);
        if (employee.AssignManager(manager.UserId, manager.Role, manager.Status,
                UserRole.Admin, UserStatus.Active).IsFailure)
            throw new InvalidOperationException("Could not assign the test employee to the manager.");
        return employee;
    }

    internal static Order CreateOrder(User manager, string name) => Order.Create(
        manager.UserId, manager.Role, manager.Status, OrderName.Create(name).Value).Value;

    internal static TaskEntity AddTask(Order order, User manager, string title) =>
        order.AddWorkItem(manager.UserId, manager.Role, manager.Status, title, "Repair description").Value;
}

internal sealed class AssignedWorkUserRepository(params User[] users) : IUserRepository
{
    public Task<User?> GetAsync(UserId id) =>
        Task.FromResult(users.SingleOrDefault(user => user.UserId == id));

    public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
    public Task AddAsync(User entity) => throw new NotSupportedException();
    public Task RemoveAsync(UserId id) => throw new NotSupportedException();
}

internal sealed class AssignedWorkOrderRepository(params Order[] orders) : IOrderRepository
{
    internal List<UserId> RequestedManagerIds { get; } = [];

    public Task<Order?> GetAsync(OrderId id) =>
        Task.FromResult(orders.SingleOrDefault(order => order.Id == id));

    public Task<IReadOnlyList<Order>> GetByManagerIdAsync(UserId managerId)
    {
        RequestedManagerIds.Add(managerId);
        // This in-memory stub supplies scoped orders; it cannot verify EF's database filter.
        return Task.FromResult<IReadOnlyList<Order>>(orders.Where(order => order.ManagerId == managerId).ToArray());
    }

    public Task<IReadOnlyList<Order>> GetAllAsync() => throw new NotSupportedException();
    public Task AddAsync(Order entity) => throw new NotSupportedException();
    public Task RemoveAsync(OrderId id) => throw new NotSupportedException();
}

internal sealed class AssignedWorkTaskRepository(params TaskEntity[] tasks) : ITaskRepository
{
    internal List<OrderId> RequestedOrderIds { get; } = [];

    public Task<TaskEntity?> GetAsync(TaskId id) =>
        Task.FromResult(tasks.SingleOrDefault(task => task.TaskId == id));

    public Task<IReadOnlyList<TaskEntity>> GetByOrderIdAsync(OrderId orderId)
    {
        RequestedOrderIds.Add(orderId);
        return Task.FromResult<IReadOnlyList<TaskEntity>>(tasks.Where(task => task.OrderId == orderId).ToArray());
    }
}
