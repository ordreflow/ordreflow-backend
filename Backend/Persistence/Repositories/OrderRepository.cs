namespace Persistence.Repositories;

using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;


public class OrderRepository(AppDbContext context)
    : GenericRepository<Order, OrderId>(context),
        IOrderRepository
{
    public override async Task<Order?> GetAsync(OrderId id)
    {
        return await Context.Orders
            .AsTracking()
            .Include(order => order.Tasks)
            .SingleOrDefaultAsync(order => order.Id == id);
    }

    public async Task<IReadOnlyList<Order>> GetByManagerIdAsync(
        UserId managerId)
    {
        return await Context.Orders
            .Where(order => order.ManagerId == managerId)
            .ToListAsync();
    }
    public async Task<IReadOnlyList<Order>> GetAllAsync()
    {
        return await Context.Orders
            .ToListAsync();
    }
}
