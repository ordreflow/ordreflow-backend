using Application.Accounts;
using Application.Commands;
using Application.Interfaces;
using Application.Queries;
using Application.WorkPlanning;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Services;
using Domain.ValueObjects;

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
        var task = planning.CreateWork(actor.Value, department, managed, catagoryId.Value, name.Value,
            description.Value, kind.Value, command.AssignmentOnly, clock.GetUtcNow().UtcDateTime);

        return task.IsFailure
            ? Result<Guid>.Failure(task.Errors.ToArray())
            : Result<Guid>.Success(task.Value.Id.Value);
    }
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

        return Result<IReadOnlyList<PlannableCategoryDto>>.Success(departments
            .SelectMany(department => department.Catagories.Select(catagory => WorkPlanningProjection.Category(department, catagory)))
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
}
