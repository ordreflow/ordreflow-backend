using Domain.Aggregate;
using Domain.ValueObjects;
using WorkTask = Domain.Entities.Task;

namespace Domain.Interfaces;

public readonly record struct TaskAssignmentAccess(bool HasAssignments, bool IsAssigned);

public interface ITimeRegistrationRepository
{
    Task<WorkTask?> GetTaskAsync(TaskId taskId, CancellationToken cancellationToken = default);
    Task<Catagory?> GetCatagoryAsync(CatagoryId catagoryId, CancellationToken cancellationToken = default);
    Task<bool> IsDepartmentMemberAsync(EmployeeId employeeId, DepartmentId departmentId, CancellationToken cancellationToken = default);
    Task<TaskAssignmentAccess> GetAssignmentAccessAsync(
        TaskId taskId, EmployeeId employeeId, CancellationToken cancellationToken = default);
    Task AddAsync(TimeEntry entry, CancellationToken cancellationToken = default);
}
