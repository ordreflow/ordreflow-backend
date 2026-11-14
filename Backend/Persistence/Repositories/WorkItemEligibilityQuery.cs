using Application.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class WorkItemEligibilityQuery(AppDbContext context) : IWorkItemEligibilityQuery
{
    public Task<WorkItemEligibility?> GetAsync(
        EmployeeId employeeId,
        TaskId taskId,
        CancellationToken cancellationToken = default) =>
        (from task in context.Tasks.AsNoTracking()
         join catagory in context.Catagories.AsNoTracking()
             on task.CatagoryId equals catagory.Id
         where task.Id == taskId
         select new WorkItemEligibility(
             task.Id,
             task.Status,
             catagory.Id,
             catagory.Status,
             catagory.DepartmentId,
             context.Employees.Any(employee =>
                 employee.Id == employeeId && employee.Status == EmployeeStatus.Active),
             context.TaskAssinments.Any(assignment =>
                 assignment.TaskId == task.Id && assignment.AssignedTo == employeeId),
             context.DepartmentEmployees.Any(membership =>
                 membership.DepartmentId == catagory.DepartmentId &&
                 membership.EmployeeId == employeeId),
             catagory.InternalOnly))
            .SingleOrDefaultAsync(cancellationToken);
}
