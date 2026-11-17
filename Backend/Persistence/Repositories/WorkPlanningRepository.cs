using Application.Interfaces;
using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class WorkPlanningRepository(AppDbContext context) : IWorkPlanningRepository
{
    public Task<Department?> GetDepartmentAsync(DepartmentId id, CancellationToken ct = default) =>
        WithWork(context.Departments).SingleOrDefaultAsync(department => department.Id == id, ct);

    public async Task<Department?> GetDepartmentByCatagoryAsync(CatagoryId id, CancellationToken ct = default)
    {
        var departmentId = await context.Catagories
            .Where(catagory => catagory.Id == id)
            .Select(catagory => catagory.DepartmentId)
            .SingleOrDefaultAsync(ct);

        return departmentId is null ? null : await GetDepartmentAsync(departmentId, ct);
    }

    public async Task<IReadOnlyList<DepartmentId>> GetManagedDepartmentIdsAsync(EmployeeId manager, CancellationToken ct = default) =>
        await context.DepartmentManagers
            .Where(link => link.ManagerId == manager)
            .Select(link => link.DepartmentId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Department>> ListDepartmentsWithWorkAsync(
        IReadOnlyCollection<DepartmentId>? only, CancellationToken ct = default)
    {
        var departments = await WithWork(context.Departments.AsNoTracking()).ToListAsync(ct);
        return only is null ? departments : departments.Where(department => only.Contains(department.Id)).ToList();
    }

    private static IQueryable<Department> WithWork(IQueryable<Department> departments) =>
        departments
            .Include(department => department.Catagories)
            .ThenInclude(catagory => catagory.Tasks);
}
