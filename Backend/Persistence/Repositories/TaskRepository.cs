using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using TaskEntity = Domain.Entities.Task;

namespace Persistence.Repositories;

public sealed class TaskRepository(AppDbContext context)
    : GenericRepository<TaskEntity, TaskId>(context), ITaskRepository
{
    public async System.Threading.Tasks.Task<IReadOnlyList<TaskEntity>> GetByOrderIdAsync(
        OrderId orderId)
    {
        return await Context.Tasks
            .Where(task => task.OrderId == orderId)
            .ToListAsync();
    }
}
