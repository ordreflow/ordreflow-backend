using Domain.ValueObjects;
using TaskEntity = Domain.Entities.Task;

namespace Domain.Interfaces;

public interface ITaskRepository
{
    System.Threading.Tasks.Task<TaskEntity?> GetAsync(TaskId id);

    System.Threading.Tasks.Task<IReadOnlyList<TaskEntity>> GetByOrderIdAsync(OrderId orderId);
}