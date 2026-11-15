using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class TimeRegistrationRepository(AppDbContext context) : ITimeRegistrationRepository
{
    public Task<Department?> GetDepartmentForTaskAsync(TaskId taskId, CancellationToken cancellationToken = default) =>
        context.Departments.AsNoTracking()
            .Include(department => department.Catagories)
            .ThenInclude(catagory => catagory.Tasks)
            .AsSplitQuery()
            .SingleOrDefaultAsync(department => department.Catagories.Any(catagory =>
                catagory.Tasks.Any(task => task.Id == taskId)), cancellationToken);

    public Task<bool> IsDepartmentMemberAsync(
        EmployeeId employeeId, DepartmentId departmentId, CancellationToken cancellationToken = default) =>
        context.DepartmentEmployees.AsNoTracking().AnyAsync(
            member => member.EmployeeId == employeeId && member.DepartmentId == departmentId,
            cancellationToken);

    public async Task<TaskAssignmentAccess> GetAssignmentAccessAsync(
        TaskId taskId, EmployeeId employeeId, CancellationToken cancellationToken = default)
    {
        var assignees = await context.TaskAssinments.AsNoTracking()
            .Where(assignment => assignment.TaskId == taskId)
            .Select(assignment => assignment.AssignedTo)
            .ToListAsync(cancellationToken);
        return new TaskAssignmentAccess(assignees.Count > 0, assignees.Contains(employeeId));
    }

    public async Task AddAsync(TimeEntry entry, CancellationToken cancellationToken = default)
    {
        await context.TimeEntries.AddAsync(entry, cancellationToken);
    }
}
