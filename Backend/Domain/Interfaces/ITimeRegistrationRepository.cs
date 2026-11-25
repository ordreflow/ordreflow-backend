using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public readonly record struct TaskAssignmentAccess(bool HasAssignments, bool IsAssigned);

public interface ITimeRegistrationRepository
{
    // Loads the owning root and its category/task children for registration rules.
    Task<Department?> GetDepartmentForTaskAsync(TaskId taskId, CancellationToken cancellationToken = default);
    Task<bool> IsDepartmentMemberAsync(EmployeeId employeeId, DepartmentId departmentId, CancellationToken cancellationToken = default);
    Task<TaskAssignmentAccess> GetAssignmentAccessAsync(
        TaskId taskId, EmployeeId employeeId, CancellationToken cancellationToken = default);
    Task AddAsync(TimeEntry entry, CancellationToken cancellationToken = default);
}
