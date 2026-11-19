using Application.Accounts;
using Application.Commands;
using Application.Interfaces;
using Application.Queries;
using Application.WorkPlanning;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.Services;
using Domain.ValueObjects;
using TaskAssignment = Domain.Entities.TaskAssignment;

namespace Application.Handlers;

public sealed class CreateCategoryHandler(
    AccountAccess access,
    IWorkPlanningRepository work,
    WorkPlanningDomainService planning,
    TimeProvider clock) : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateCategoryCommand command, CancellationToken ct = default)
    {
        var actor = await access.RequireEmployeeAsync(command.ActorId, ct);
        if (actor.IsFailure) return Result<Guid>.Failure(WorkPlanningErrors.Forbidden);

        var departmentId = DepartmentId.Create(command.DepartmentId);
        if (departmentId.IsFailure) return Result<Guid>.Failure(departmentId.Errors.ToArray());
        var name = CatagoryName.Create(command.Name);
        if (name.IsFailure) return Result<Guid>.Failure(name.Errors.ToArray());
        var description = WorkPlanningInput.Description(command.Description);
        if (description.IsFailure) return Result<Guid>.Failure(description.Errors.ToArray());

        var department = await work.GetDepartmentAsync(departmentId.Value, ct);
        if (department is null) return Result<Guid>.Failure(WorkPlanningErrors.DepartmentNotFound);

        var managed = await work.GetManagedDepartmentIdsAsync(actor.Value.Id, ct);
        var catagory = planning.CreateCatagory(actor.Value, department, managed, name.Value, command.InternalOnly,
            description.Value, clock.GetUtcNow().UtcDateTime);

        return catagory.IsFailure
            ? Result<Guid>.Failure(catagory.Errors.ToArray())
            : Result<Guid>.Success(catagory.Value.Id.Value);
    }
}

public sealed class CreateWorkItemHandler(
    AccountAccess access,
    IWorkPlanningRepository work,
    IEmployeeAccessRepository employees,
    WorkPlanningDomainService planning,
    TimeProvider clock) : ICommandHandler<CreateWorkItemCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateWorkItemCommand command, CancellationToken ct = default)
    {
        var actor = await access.RequireEmployeeAsync(command.ActorId, ct);
        if (actor.IsFailure) return Result<Guid>.Failure(WorkPlanningErrors.Forbidden);

        var catagoryId = CatagoryId.Create(command.CategoryId);
        if (catagoryId.IsFailure) return Result<Guid>.Failure(catagoryId.Errors.ToArray());
        var name = TaskName.Create(command.Name);
        if (name.IsFailure) return Result<Guid>.Failure(name.Errors.ToArray());
        var description = WorkPlanningInput.Description(command.Description);
        if (description.IsFailure) return Result<Guid>.Failure(description.Errors.ToArray());
        var kind = WorkPlanningInput.Kind(command.Kind);
        if (kind.IsFailure) return Result<Guid>.Failure(kind.Errors.ToArray());

        var department = await work.GetDepartmentByCatagoryAsync(catagoryId.Value, ct);
        if (department is null) return Result<Guid>.Failure(WorkPlanningErrors.CategoryNotFound);

        var managed = await work.GetManagedDepartmentIdsAsync(actor.Value.Id, ct);
        var planningAccess = planning.CanPlanIn(actor.Value, department, managed);
        if (planningAccess.IsFailure)
            return Result<Guid>.Failure(WorkPlanningErrors.Forbidden);

        var prepared = await WorkAssignmentInput.PrepareAsync(command.Assignments, department, work, employees, ct);
        if (prepared.IsFailure) return Result<Guid>.Failure(prepared.Errors.ToArray());

        var task = planning.CreateWork(actor.Value, department, managed, catagoryId.Value, name.Value,
            description.Value, kind.Value, command.AssignmentOnly, clock.GetUtcNow().UtcDateTime);

        if (task.IsFailure) return Result<Guid>.Failure(task.Errors.ToArray());

        var taskAssignments = WorkAssignmentInput.ToAssignments(task.Value.Id, prepared.Value);
        if (taskAssignments.Count > 0)
            await work.AddAssignmentsAsync(taskAssignments, ct);

        return Result<Guid>.Success(task.Value.Id.Value);
    }
}

public sealed class UpdateCategoryHandler(
    AccountAccess access,
    IWorkPlanningRepository work,
    WorkPlanningDomainService planning) : ICommandHandler<UpdateCategoryCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(UpdateCategoryCommand command, CancellationToken ct = default)
    {
        var actor = await access.RequireEmployeeAsync(command.ActorId, ct);
        if (actor.IsFailure) return Result<bool>.Failure(WorkPlanningErrors.Forbidden);

        var catagoryId = CatagoryId.Create(command.CategoryId);
        if (catagoryId.IsFailure) return Result<bool>.Failure(catagoryId.Errors.ToArray());
        var name = CatagoryName.Create(command.Name);
        if (name.IsFailure) return Result<bool>.Failure(name.Errors.ToArray());
        var description = WorkPlanningInput.Description(command.Description);
        if (description.IsFailure) return Result<bool>.Failure(description.Errors.ToArray());

        var department = await work.GetDepartmentByCatagoryAsync(catagoryId.Value, ct);
        if (department is null) return Result<bool>.Failure(WorkPlanningErrors.CategoryNotFound);

        var managed = await work.GetManagedDepartmentIdsAsync(actor.Value.Id, ct);
        var updated = planning.UpdateCatagory(actor.Value, department, managed, catagoryId.Value, name.Value, description.Value);
        return updated.IsFailure ? Result<bool>.Failure(updated.Errors.ToArray()) : Result<bool>.Success(true);
    }
}

public sealed class UpdateWorkItemHandler(
    AccountAccess access,
    IWorkPlanningRepository work,
    IEmployeeAccessRepository employees,
    WorkPlanningDomainService planning) : ICommandHandler<UpdateWorkItemCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(UpdateWorkItemCommand command, CancellationToken ct = default)
    {
        var actor = await access.RequireEmployeeAsync(command.ActorId, ct);
        if (actor.IsFailure) return Result<bool>.Failure(WorkPlanningErrors.Forbidden);

        var taskId = TaskId.Create(command.WorkItemId);
        if (taskId.IsFailure) return Result<bool>.Failure(taskId.Errors.ToArray());
        var name = TaskName.Create(command.Name);
        if (name.IsFailure) return Result<bool>.Failure(name.Errors.ToArray());
        var description = WorkPlanningInput.Description(command.Description);
        if (description.IsFailure) return Result<bool>.Failure(description.Errors.ToArray());
        var kind = WorkPlanningInput.Kind(command.Kind);
        if (kind.IsFailure) return Result<bool>.Failure(kind.Errors.ToArray());

        var department = await work.GetDepartmentByTaskAsync(taskId.Value, ct);
        if (department is null) return Result<bool>.Failure(WorkPlanningErrors.WorkItemNotFound);

        var managed = await work.GetManagedDepartmentIdsAsync(actor.Value.Id, ct);
        if (planning.CanPlanIn(actor.Value, department, managed).IsFailure)
            return Result<bool>.Failure(WorkPlanningErrors.Forbidden);

        var prepared = await WorkAssignmentInput.PrepareAsync(command.Assignments, department, work, employees, ct);
        if (prepared.IsFailure) return Result<bool>.Failure(prepared.Errors.ToArray());

        var updated = planning.UpdateWork(actor.Value, department, managed, taskId.Value, name.Value,
            description.Value, kind.Value, command.AssignmentOnly);
        if (updated.IsFailure) return Result<bool>.Failure(updated.Errors.ToArray());

        await work.ReplaceAssignmentsAsync(taskId.Value, WorkAssignmentInput.ToAssignments(taskId.Value, prepared.Value), ct);
        return Result<bool>.Success(true);
    }
}

internal static class WorkAssignmentInput
{
    public sealed record Prepared(EmployeeId EmployeeId, PlannedDuration Duration, TargetCompletionDate Deadline);

    /// <summary>Validates assignees: active employees, each once, all members of the owning department.</summary>
    public static async Task<Result<IReadOnlyList<Prepared>>> PrepareAsync(
        IReadOnlyList<WorkItemAssignmentInput>? assignments, Department department,
        IWorkPlanningRepository work, IEmployeeAccessRepository employees, CancellationToken ct)
    {
        var seen = new HashSet<Guid>();
        var prepared = new List<Prepared>();
        foreach (var assignment in assignments ?? [])
        {
            if (assignment.EmployeeId == Guid.Empty)
                return Fail(new Error("AssigneeRequired", "An employee is required for each assignment."));
            if (!seen.Add(assignment.EmployeeId))
                return Fail(new Error("DuplicateAssignee", "An employee can only be assigned once to a work item."));

            var employeeId = EmployeeId.Create(assignment.EmployeeId).Value;
            var assignee = await employees.GetWithAccessesAsync(employeeId, ct);
            if (assignee is null || !assignee.HasPermission(SystemPermission.Employee))
                return Fail(new Error("AssigneeUnavailable", "The assignee must be an active employee."));
            if (!await work.IsDepartmentMemberAsync(employeeId, department.Id, ct))
                return Fail(new Error("AssigneeDepartmentForbidden", "Employees can only be assigned work in their department."));

            var duration = PlannedDuration.Create(assignment.ExpectedHours);
            if (duration.IsFailure) return Fail(duration.Errors.ToArray());
            var deadline = TargetCompletionDate.Create(assignment.ExpectedDeadline);
            if (deadline.IsFailure) return Fail(deadline.Errors.ToArray());

            prepared.Add(new Prepared(employeeId, duration.Value, deadline.Value));
        }

        return Result<IReadOnlyList<Prepared>>.Success(prepared);
    }

    public static IReadOnlyList<TaskAssignment> ToAssignments(TaskId taskId, IReadOnlyList<Prepared> prepared) =>
        prepared
            .Select(item => TaskAssignment.Create(taskId, item.EmployeeId, item.Duration, item.Deadline).Value)
            .ToArray();

    private static Result<IReadOnlyList<Prepared>> Fail(params Error[] errors) =>
        Result<IReadOnlyList<Prepared>>.Failure(errors);
}

public sealed class ListPlannableCategoriesHandler(AccountAccess access, IWorkPlanningRepository work)
    : IQueryHandler<ListPlannableCategoriesQuery, Result<IReadOnlyList<PlannableCategoryDto>>>
{
    public async Task<Result<IReadOnlyList<PlannableCategoryDto>>> HandleAsync(ListPlannableCategoriesQuery query)
    {
        var actor = await access.RequireEmployeeAsync(query.ActorId);
        if (actor.IsFailure) return Result<IReadOnlyList<PlannableCategoryDto>>.Failure(WorkPlanningErrors.Forbidden);
        var me = actor.Value;

        IReadOnlyList<Department> departments;
        if (me.HasPermission(SystemPermission.Administrator))
            departments = await work.ListDepartmentsWithWorkAsync(null);
        else if (DepartmentStaffingDomainService.HasManagerAccess(me))
            departments = await work.ListDepartmentsWithWorkAsync(await work.GetManagedDepartmentIdsAsync(me.Id));
        else
            return Result<IReadOnlyList<PlannableCategoryDto>>.Failure(WorkPlanningErrors.Forbidden);

        var taskIds = departments
            .SelectMany(department => department.Catagories)
            .SelectMany(catagory => catagory.Tasks)
            .Select(task => task.Id)
            .ToArray();
        var assignments = (await work.ListAssignmentsAsync(taskIds)).ToLookup(assignment => assignment.TaskId);

        return Result<IReadOnlyList<PlannableCategoryDto>>.Success(departments
            .SelectMany(department => department.Catagories.Select(catagory =>
                WorkPlanningProjection.Category(department, catagory, assignments)))
            .OrderBy(category => category.DepartmentName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray());
    }
}

internal static class WorkPlanningErrors
{
    public static readonly Error Forbidden = new("WorkPlanningForbidden",
        "Only a manager of the owning department or an administrator can plan work here.");
    public static readonly Error DepartmentNotFound = new("DepartmentNotFound", "The department could not be found.");
    public static readonly Error CategoryNotFound = new("CategoryNotFound", "The category could not be found.");
    public static readonly Error WorkItemNotFound = new("WorkItemNotFound", "The work item could not be found.");
}
