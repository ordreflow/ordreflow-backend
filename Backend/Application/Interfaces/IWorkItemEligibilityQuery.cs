using Domain.ValueObjects;

namespace Application.Interfaces;

public interface IWorkItemEligibilityQuery
{
    Task<WorkItemEligibility?> GetAsync(
        EmployeeId employeeId,
        TaskId taskId,
        CancellationToken cancellationToken = default);
}

public sealed record WorkItemEligibility(
    TaskId TaskId,
    Domain.ValueObjects.TaskStatus TaskStatus,
    CatagoryId CatagoryId,
    CatagoryStatus CatagoryStatus,
    DepartmentId DepartmentId,
    bool IsEmployeeActive,
    bool IsAssignedToEmployee,
    bool IsEmployeeInDepartment,
    bool IsInternalOnly);
