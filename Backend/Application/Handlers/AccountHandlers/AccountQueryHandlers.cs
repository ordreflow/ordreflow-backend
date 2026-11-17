using Application.Accounts;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetMyProfileHandler(AccountAccess access) : IQueryHandler<GetMyProfileQuery, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> HandleAsync(GetMyProfileQuery query)
    {
        var actor = await access.RequireEmployeeAsync(query.ActorId);
        return actor.IsFailure
            ? Result<EmployeeDto>.Failure(actor.Errors.ToArray())
            : Result<EmployeeDto>.Success(AccountProjection.Employee(actor.Value));
    }
}

public sealed class ListEmployeesHandler(AccountAccess access, IAccountRepository accounts)
    : IQueryHandler<ListEmployeesQuery, Result<IReadOnlyList<EmployeeDto>>>
{
    public async Task<Result<IReadOnlyList<EmployeeDto>>> HandleAsync(ListEmployeesQuery query)
    {
        var admin = await access.RequireAdministratorAsync(query.ActorId);
        if (admin.IsFailure) return Result<IReadOnlyList<EmployeeDto>>.Failure(admin.Errors.ToArray());

        var employees = await accounts.ListEmployeesAsync();
        return Result<IReadOnlyList<EmployeeDto>>.Success(employees
            .OrderBy(employee => employee.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(AccountProjection.Employee)
            .ToArray());
    }
}

public sealed class GetEmployeeHandler(AccountAccess access, IAccountRepository accounts)
    : IQueryHandler<GetEmployeeQuery, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> HandleAsync(GetEmployeeQuery query)
    {
        var admin = await access.RequireAdministratorAsync(query.ActorId);
        if (admin.IsFailure) return Result<EmployeeDto>.Failure(admin.Errors.ToArray());

        var id = EmployeeId.Create(query.EmployeeId);
        if (id.IsFailure) return Result<EmployeeDto>.Failure(id.Errors.ToArray());

        var employee = await accounts.GetEmployeeAsync(id.Value);
        return employee is null
            ? Result<EmployeeDto>.Failure(AccountInput.EmployeeNotFound)
            : Result<EmployeeDto>.Success(AccountProjection.Employee(employee));
    }
}

public sealed class ListDepartmentsHandler(AccountAccess access, IAccountRepository accounts)
    : IQueryHandler<ListDepartmentsQuery, Result<IReadOnlyList<DepartmentDto>>>
{
    public async Task<Result<IReadOnlyList<DepartmentDto>>> HandleAsync(ListDepartmentsQuery query)
    {
        var actor = await access.RequireEmployeeAsync(query.ActorId);
        if (actor.IsFailure) return Result<IReadOnlyList<DepartmentDto>>.Failure(actor.Errors.ToArray());
        var me = actor.Value;

        var isAdmin = me.HasPermission(SystemPermission.Administrator);
        var canManage = DepartmentStaffingDomainService.HasManagerAccess(me);
        var people = (await accounts.ListEmployeesAsync()).ToDictionary(employee => employee.Id);
        var memberships = await accounts.ListMembershipsAsync();
        var managements = await accounts.ListManagementsAsync();

        var result = new List<DepartmentDto>();
        foreach (var department in await accounts.ListDepartmentsAsync())
        {
            var members = memberships.Where(link => link.DepartmentId == department.Id).Select(link => link.EmployeeId).ToArray();
            var managers = managements.Where(link => link.DepartmentId == department.Id).Select(link => link.ManagerId).ToArray();
            var managedByMe = canManage && managers.Contains(me.Id);
            var isMember = members.Contains(me.Id) || managers.Contains(me.Id);

            if (!isAdmin && !isMember)
                continue;

            // Admins and the department's managers see everyone; other members only see themselves (SEC-02).
            var seeAll = isAdmin || managedByMe;
            result.Add(new DepartmentDto(
                department.Id.Value,
                department.Name.Value,
                managedByMe,
                People(managers, people),
                People(seeAll ? members : members.Where(id => id == me.Id), people)));
        }

        return Result<IReadOnlyList<DepartmentDto>>.Success(result
            .OrderBy(department => department.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray());
    }

    private static IReadOnlyList<DepartmentPersonDto> People(
        IEnumerable<EmployeeId> ids, IReadOnlyDictionary<EmployeeId, Employee> people) =>
        ids.Where(people.ContainsKey)
            .Select(id => AccountProjection.Person(people[id]))
            .OrderBy(person => person.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
}

public sealed class ListManagedEmployeesHandler(AccountAccess access, IAccountRepository accounts)
    : IQueryHandler<ListManagedEmployeesQuery, Result<IReadOnlyList<DepartmentPersonDto>>>
{
    public async Task<Result<IReadOnlyList<DepartmentPersonDto>>> HandleAsync(ListManagedEmployeesQuery query)
    {
        var actor = await access.RequireEmployeeAsync(query.ActorId);
        if (actor.IsFailure) return Result<IReadOnlyList<DepartmentPersonDto>>.Failure(actor.Errors.ToArray());
        var me = actor.Value;

        // The DepartmentManager grant is required, not just a manager link or a title (SEC-01).
        if (!DepartmentStaffingDomainService.HasManagerAccess(me))
            return Result<IReadOnlyList<DepartmentPersonDto>>.Failure(
                new Error("ManagedEmployeesForbidden", "Active department-manager access is required."));

        var managed = (await accounts.ListManagementsAsync())
            .Where(link => link.ManagerId == me.Id)
            .Select(link => link.DepartmentId)
            .ToHashSet();
        var memberIds = (await accounts.ListMembershipsAsync())
            .Where(link => managed.Contains(link.DepartmentId) && link.EmployeeId != me.Id)
            .Select(link => link.EmployeeId)
            .ToHashSet();

        var employees = (await accounts.ListEmployeesAsync())
            .Where(employee => memberIds.Contains(employee.Id) && employee.Status == EmployeeStatus.Active)
            .OrderBy(employee => employee.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(AccountProjection.Person)
            .ToArray();

        return Result<IReadOnlyList<DepartmentPersonDto>>.Success(employees);
    }
}
