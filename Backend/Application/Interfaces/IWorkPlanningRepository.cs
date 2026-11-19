using Domain.Aggregate;
using Domain.ValueObjects;
using CatagoryReviewer = Domain.Entities.CatagoryReviewer;
using TaskAssignment = Domain.Entities.TaskAssignment;

namespace Application.Interfaces;

/// <summary>Departments with their categories and work, for planning work (UC-08).</summary>
public interface IWorkPlanningRepository
{
    /// <summary>A tracked department with its categories and tasks.</summary>
    Task<Department?> GetDepartmentAsync(DepartmentId id, CancellationToken ct = default);

    /// <summary>The tracked department that owns the category, with its categories and tasks.</summary>
    Task<Department?> GetDepartmentByCatagoryAsync(CatagoryId id, CancellationToken ct = default);

    Task<IReadOnlyList<DepartmentId>> GetManagedDepartmentIdsAsync(EmployeeId manager, CancellationToken ct = default);

    Task<bool> IsDepartmentMemberAsync(EmployeeId employee, DepartmentId department, CancellationToken ct = default);

    /// <summary>Whether the employee is a member or a manager of the department.</summary>
    Task<bool> IsDepartmentStaffAsync(EmployeeId employee, DepartmentId department, CancellationToken ct = default);

    Task<IReadOnlyList<CatagoryReviewer>> ListReviewersAsync(IReadOnlyCollection<CatagoryId> catagories, CancellationToken ct = default);

    /// <summary>Replaces every reviewer of the category with the given ones.</summary>
    Task ReplaceReviewersAsync(CatagoryId catagory, IReadOnlyCollection<CatagoryReviewer> reviewers, CancellationToken ct = default);

    Task AddAssignmentsAsync(IReadOnlyCollection<TaskAssignment> assignments, CancellationToken ct = default);

    /// <summary>The tracked department that owns the task, with its categories and tasks.</summary>
    Task<Department?> GetDepartmentByTaskAsync(TaskId id, CancellationToken ct = default);

    Task<IReadOnlyList<TaskAssignment>> ListAssignmentsAsync(IReadOnlyCollection<TaskId> tasks, CancellationToken ct = default);

    /// <summary>Replaces every assignment on the task with the given ones.</summary>
    Task ReplaceAssignmentsAsync(TaskId task, IReadOnlyCollection<TaskAssignment> assignments, CancellationToken ct = default);

    /// <summary>Read-only departments with categories and tasks; all departments when <paramref name="only"/> is null.</summary>
    Task<IReadOnlyList<Department>> ListDepartmentsWithWorkAsync(IReadOnlyCollection<DepartmentId>? only, CancellationToken ct = default);
}
