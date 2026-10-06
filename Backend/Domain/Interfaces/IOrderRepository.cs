using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IOrderRepository
    : IGenericRepository<Order, OrderId>
{
    Task<IReadOnlyList<Order>> GetByManagerIdAsync(
        UserId managerId);
}