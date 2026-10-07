using Domain.ValueObjects;
using TaskEntity = Domain.Entities.Task;

namespace Domain.Interfaces;

public interface ITaskRepository : IGenericRepository<TaskEntity, TaskId>
{
    System.Threading.Tasks.Task<IReadOnlyList<TaskEntity>> GetByOrderIdAsync(OrderId orderId);
}