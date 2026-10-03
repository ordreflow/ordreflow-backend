namespace Persistence.Repositories;

using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;


public class OrderRepository(AppDbContext context)
    : GenericRepository<Order, OrderId>(context),
        IOrderRepository
{
    public async Task<IReadOnlyList<Order>> GetByManagerIdAsync(
        UserId managerId)
    {
        // return await Context.Orders
        //     .Where(order => order.ManagerId == managerId)
        //     .ToListAsync();
        throw  new NotImplementedException();
    }
}