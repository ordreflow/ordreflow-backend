using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Interfaces;

/// <summary>Departments with their categories and work, for planning work (UC-08).</summary>
public interface IWorkPlanningRepository
{
    /// <summary>A tracked department with its categories and tasks.</summary>
    Task<Department?> GetDepartmentAsync(DepartmentId id, CancellationToken ct = default);

    /// <summary>The tracked department that owns the category, with its categories and tasks.</summary>
    Task<Department?> GetDepartmentByCatagoryAsync(CatagoryId id, CancellationToken ct = default);

    Task<IReadOnlyList<DepartmentId>> GetManagedDepartmentIdsAsync(EmployeeId manager, CancellationToken ct = default);

    /// <summary>Read-only departments with categories and tasks; all departments when <paramref name="only"/> is null.</summary>
    Task<IReadOnlyList<Department>> ListDepartmentsWithWorkAsync(IReadOnlyCollection<DepartmentId>? only, CancellationToken ct = default);
}
