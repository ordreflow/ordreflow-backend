using Application.Interfaces;
using Domain.Aggregate;
using Domain.Entities;
using Task = System.Threading.Tasks.Task;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class AccountRepository(AppDbContext context) : IAccountRepository
{
    public Task<Employee?> GetEmployeeAsync(EmployeeId id, CancellationToken ct = default) =>
        context.Employees
            .Include(employee => employee.Accesses)
            .SingleOrDefaultAsync(employee => employee.Id == id, ct);

    public async Task<IReadOnlyList<Employee>> ListEmployeesAsync(CancellationToken ct = default) =>
        await context.Employees
            .AsNoTracking()
            .Include(employee => employee.Accesses)
            .ToListAsync(ct);

    public Task<bool> EmailInUseAsync(EmailAddress email, CancellationToken ct = default) =>
        context.Employees.AnyAsync(employee => employee.Email == email, ct);

    public async Task AddEmployeeAsync(Employee employee, CancellationToken ct = default) =>
        await context.Employees.AddAsync(employee, ct);

    public Task<Department?> GetDepartmentAsync(DepartmentId id, CancellationToken ct = default) =>
        context.Departments.SingleOrDefaultAsync(department => department.Id == id, ct);

    public async Task<IReadOnlyList<Department>> ListDepartmentsAsync(CancellationToken ct = default) =>
        await context.Departments.AsNoTracking().ToListAsync(ct);

    public Task<bool> DepartmentNameInUseAsync(DepartmentName name, CancellationToken ct = default) =>
        context.Departments.AnyAsync(department => department.Name == name, ct);

    public async Task AddDepartmentAsync(Department department, CancellationToken ct = default) =>
        await context.Departments.AddAsync(department, ct);

    public async Task<IReadOnlyList<DepartmentEmployee>> ListMembershipsAsync(CancellationToken ct = default) =>
        await context.DepartmentEmployees.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<DepartmentManager>> ListManagementsAsync(CancellationToken ct = default) =>
        await context.DepartmentManagers.AsNoTracking().ToListAsync(ct);

    public Task<DepartmentEmployee?> GetMembershipAsync(DepartmentId department, EmployeeId employee, CancellationToken ct = default) =>
        context.DepartmentEmployees.SingleOrDefaultAsync(
            link => link.DepartmentId == department && link.EmployeeId == employee, ct);

    public Task<DepartmentManager?> GetManagementAsync(DepartmentId department, EmployeeId manager, CancellationToken ct = default) =>
        context.DepartmentManagers.SingleOrDefaultAsync(
            link => link.DepartmentId == department && link.ManagerId == manager, ct);

    public async Task AddMembershipAsync(DepartmentEmployee membership, CancellationToken ct = default) =>
        await context.DepartmentEmployees.AddAsync(membership, ct);

    public async Task AddManagementAsync(DepartmentManager management, CancellationToken ct = default) =>
        await context.DepartmentManagers.AddAsync(management, ct);

    public void RemoveMembership(DepartmentEmployee membership) => context.DepartmentEmployees.Remove(membership);

    public void RemoveManagement(DepartmentManager management) => context.DepartmentManagers.Remove(management);
}
