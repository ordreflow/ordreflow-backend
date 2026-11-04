using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR08;

internal static class CreationTestData
{
    internal static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    internal static Order CreateOrder(User manager) => Order.Create(
        manager.UserId, manager.Role, manager.Status, OrderName.Create("Repair order").Value).Value;
}

internal sealed class CreationUserRepository(params User[] users) : IUserRepository
{
    internal List<UserId> RequestedIds { get; } = [];

    public Task<User?> GetAsync(UserId id)
    {
        RequestedIds.Add(id);
        return Task.FromResult(users.SingleOrDefault(user => user.UserId == id));
    }

    public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
    public Task AddAsync(User entity) => throw new NotSupportedException();
    public Task RemoveAsync(UserId id) => throw new NotSupportedException();
}

internal sealed class CreationOrderRepository(params Order[] initialOrders) : IOrderRepository
{
    private readonly List<Order> _orders = [.. initialOrders];
    private readonly List<Order> _added = [];

    internal IReadOnlyList<Order> Added => _added;
    internal List<OrderId> RequestedIds { get; } = [];

    public Task<Order?> GetAsync(OrderId id)
    {
        RequestedIds.Add(id);
        return Task.FromResult(_orders.SingleOrDefault(order => order.Id == id));
    }

    public Task AddAsync(Order entity)
    {
        _orders.Add(entity);
        _added.Add(entity);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Order>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
    public Task<IReadOnlyList<Order>> GetAllAsync() => throw new NotSupportedException();
    public Task RemoveAsync(OrderId id) => throw new NotSupportedException();
}
