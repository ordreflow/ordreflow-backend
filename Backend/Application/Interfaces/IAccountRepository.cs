using Domain.Aggregate;
using Domain.Entities;
using Task = System.Threading.Tasks.Task;
using Domain.ValueObjects;

namespace Application.Interfaces;

/// <summary>Employees, departments and the links between them, for account and access administration (UC-05).</summary>
public interface IAccountRepository
{
    /// <summary>Loads a tracked employee with its system accesses.</summary>
    Task<Employee?> GetEmployeeAsync(EmployeeId id, CancellationToken ct = default);

    Task<IReadOnlyList<Employee>> ListEmployeesAsync(CancellationToken ct = default);

    Task<bool> EmailInUseAsync(EmailAddress email, CancellationToken ct = default);

    Task AddEmployeeAsync(Employee employee, CancellationToken ct = default);

    Task<Department?> GetDepartmentAsync(DepartmentId id, CancellationToken ct = default);

    Task<IReadOnlyList<Department>> ListDepartmentsAsync(CancellationToken ct = default);

    Task<bool> DepartmentNameInUseAsync(DepartmentName name, CancellationToken ct = default);

    Task AddDepartmentAsync(Department department, CancellationToken ct = default);

    Task<IReadOnlyList<DepartmentEmployee>> ListMembershipsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<DepartmentManager>> ListManagementsAsync(CancellationToken ct = default);

    Task<DepartmentEmployee?> GetMembershipAsync(DepartmentId department, EmployeeId employee, CancellationToken ct = default);

    Task<DepartmentManager?> GetManagementAsync(DepartmentId department, EmployeeId manager, CancellationToken ct = default);

    Task AddMembershipAsync(DepartmentEmployee membership, CancellationToken ct = default);

    Task AddManagementAsync(DepartmentManager management, CancellationToken ct = default);

    void RemoveMembership(DepartmentEmployee membership);

    void RemoveManagement(DepartmentManager management);
}
