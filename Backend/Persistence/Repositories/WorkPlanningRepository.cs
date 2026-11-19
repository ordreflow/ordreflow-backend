using Application.Interfaces;
using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using TaskAssignment = Domain.Entities.TaskAssignment;

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

    public Task<bool> IsDepartmentMemberAsync(EmployeeId employee, DepartmentId department, CancellationToken ct = default) =>
        context.DepartmentEmployees.AsNoTracking().AnyAsync(
            member => member.EmployeeId == employee && member.DepartmentId == department, ct);

    public Task AddAssignmentsAsync(IReadOnlyCollection<TaskAssignment> assignments, CancellationToken ct = default) =>
        context.TaskAssinments.AddRangeAsync(assignments, ct);

    public async Task<Department?> GetDepartmentByTaskAsync(TaskId id, CancellationToken ct = default)
    {
        var departmentId = await (
                from task in context.Tasks
                join catagory in context.Catagories on task.CatagoryId equals catagory.Id
                where task.Id == id
                select catagory.DepartmentId)
            .SingleOrDefaultAsync(ct);

        return departmentId is null ? null : await GetDepartmentAsync(departmentId, ct);
    }

    public async Task<IReadOnlyList<TaskAssignment>> ListAssignmentsAsync(
        IReadOnlyCollection<TaskId> tasks, CancellationToken ct = default) =>
        tasks.Count == 0
            ? []
            : await context.TaskAssinments.AsNoTracking()
                .Where(assignment => tasks.Contains(assignment.TaskId))
                .ToListAsync(ct);

    public async Task ReplaceAssignmentsAsync(
        TaskId task, IReadOnlyCollection<TaskAssignment> assignments, CancellationToken ct = default)
    {
        var existing = await context.TaskAssinments
            .Where(assignment => assignment.TaskId == task)
            .ToListAsync(ct);
        var wanted = assignments.ToDictionary(assignment => assignment.AssignedTo);

        foreach (var current in existing)
        {
            if (wanted.Remove(current.AssignedTo, out var update))
            {
                current.ExpectedDuration = update.ExpectedDuration;
                current.ExpectedDeadline = update.ExpectedDeadline;
            }
            else
            {
                context.TaskAssinments.Remove(current);
            }
        }

        await context.TaskAssinments.AddRangeAsync(wanted.Values, ct);
    }

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
