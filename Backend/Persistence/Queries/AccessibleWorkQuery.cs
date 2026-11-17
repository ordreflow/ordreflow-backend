using Application.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Persistence.Queries;

/// <summary>
/// Read model for the work an employee may select. The filter is the same as
/// <see cref="Domain.Services.WorkSelectionRules"/> and runs in the database, so hidden work is never loaded.
/// </summary>
public sealed class AccessibleWorkQuery(AppDbContext context) : IAccessibleWorkQuery
{
    public async Task<IReadOnlyList<AccessibleWorkDto>> ListAsync(EmployeeId employee, bool assignedToMeOnly, CancellationToken ct = default) =>
        (await Selectable(employee, assignedToMeOnly, null).ToListAsync(ct)).Select(ToDto).ToArray();

    public async Task<AccessibleWorkDto?> GetAsync(EmployeeId employee, TaskId task, CancellationToken ct = default)
    {
        var row = await Selectable(employee, false, task).SingleOrDefaultAsync(ct);
        return row is null ? null : ToDto(row);
    }

    private sealed record Row(Domain.Entities.Task Task, Domain.Entities.Catagory Catagory,
        Domain.Aggregate.Department Department, Domain.Entities.TaskAssignment? Mine);

    // The filtering runs in the database; only the selected rows are mapped here.
    private static AccessibleWorkDto ToDto(Row row) => new(
        row.Task.Id.Value,
        row.Task.Name.Value,
        row.Task.Description,
        row.Task.Kind.ToString(),
        row.Task.AssignmentOnly,
        row.Mine is not null,
        row.Mine?.ExpectedDuration.Value,
        row.Mine?.ExpectedDeadline.Value,
        row.Catagory.Id.Value,
        row.Catagory.Name.Value,
        row.Department.Id.Value,
        row.Department.Name.Value);

    private IQueryable<Row> Selectable(EmployeeId employee, bool assignedToMeOnly, TaskId? only)
    {
        var memberOf = context.DepartmentEmployees.AsNoTracking()
            .Where(member => member.EmployeeId == employee)
            .Select(member => member.DepartmentId);
        var assignments = context.TaskAssinments.AsNoTracking();

        var rows =
            from task in context.Tasks.AsNoTracking()
            join catagory in context.Catagories.AsNoTracking() on task.CatagoryId equals catagory.Id
            join department in context.Departments.AsNoTracking() on catagory.DepartmentId equals department.Id
            let mine = assignments.FirstOrDefault(assignment => assignment.TaskId == task.Id && assignment.AssignedTo == employee)
            let hasAssignments = assignments.Any(assignment => assignment.TaskId == task.Id)
            where catagory.Status == CatagoryStatus.Active && task.Status == TaskStatus.Active
            where !catagory.InternalOnly || memberOf.Contains(catagory.DepartmentId)
            where mine != null || (!task.AssignmentOnly && !hasAssignments)
            where !assignedToMeOnly || mine != null
            select new { task, catagory, department, mine };

        if (only is not null)
            rows = rows.Where(row => row.task.Id == only);

        return rows.Select(row => new Row(row.task, row.catagory, row.department, row.mine));
    }
}
