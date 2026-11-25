using Application.Accounts;
using Application.Commands;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateDepartmentHandler(AccountAccess access, IAccountRepository accounts)
    : ICommandHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateDepartmentCommand command, CancellationToken ct = default)
    {
        var admin = await access.RequireAdministratorAsync(command.ActorId, ct);
        if (admin.IsFailure) return Result<Guid>.Failure(admin.Errors.ToArray());

        var name = DepartmentName.Create(command.Name);
        if (name.IsFailure) return Result<Guid>.Failure(name.Errors.ToArray());
        if (await accounts.DepartmentNameInUseAsync(name.Value, ct))
            return Result<Guid>.Failure(new Error("DepartmentNameTaken", "A department with this name already exists."));

        var department = Department.Create(name.Value);
        if (department.IsFailure) return Result<Guid>.Failure(department.Errors.ToArray());

        await accounts.AddDepartmentAsync(department.Value, ct);
        return Result<Guid>.Success(department.Value.Id.Value);
    }
}

public sealed class AddDepartmentEmployeeHandler(
    AccountAccess access, IAccountRepository accounts, DepartmentStaffingDomainService staffing)
    : ICommandHandler<AddDepartmentEmployeeCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(AddDepartmentEmployeeCommand command, CancellationToken ct = default)
    {
        var loaded = await DepartmentLinks.LoadAsync(access, accounts, command.ActorId, command.DepartmentId, command.EmployeeId, ct);
        if (loaded.IsFailure) return Result<bool>.Failure(loaded.Errors.ToArray());
        var (department, employee) = loaded.Value;

        if (await accounts.GetMembershipAsync(department.Id, employee.Id, ct) is not null)
            return Result<bool>.Success(true);

        var membership = staffing.AddMember(department, employee);
        if (membership.IsFailure) return Result<bool>.Failure(membership.Errors.ToArray());

        await accounts.AddMembershipAsync(membership.Value, ct);
        return Result<bool>.Success(true);
    }
}

public sealed class RemoveDepartmentEmployeeHandler(AccountAccess access, IAccountRepository accounts)
    : ICommandHandler<RemoveDepartmentEmployeeCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(RemoveDepartmentEmployeeCommand command, CancellationToken ct = default)
    {
        var loaded = await DepartmentLinks.LoadAsync(access, accounts, command.ActorId, command.DepartmentId, command.EmployeeId, ct);
        if (loaded.IsFailure) return Result<bool>.Failure(loaded.Errors.ToArray());
        var (department, employee) = loaded.Value;

        var membership = await accounts.GetMembershipAsync(department.Id, employee.Id, ct);
        if (membership is null)
            return Result<bool>.Failure(new Error("DepartmentMembershipNotFound", "The employee is not a member of the department."));

        accounts.RemoveMembership(membership);
        return Result<bool>.Success(true);
    }
}

public sealed class AppointDepartmentManagerHandler(
    AccountAccess access, IAccountRepository accounts, DepartmentStaffingDomainService staffing)
    : ICommandHandler<AppointDepartmentManagerCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(AppointDepartmentManagerCommand command, CancellationToken ct = default)
    {
        var loaded = await DepartmentLinks.LoadAsync(access, accounts, command.ActorId, command.DepartmentId, command.EmployeeId, ct);
        if (loaded.IsFailure) return Result<bool>.Failure(loaded.Errors.ToArray());
        var (department, employee) = loaded.Value;

        if (await accounts.GetManagementAsync(department.Id, employee.Id, ct) is not null)
            return Result<bool>.Success(true);

        var management = staffing.AppointManager(department, employee);
        if (management.IsFailure) return Result<bool>.Failure(management.Errors.ToArray());

        await accounts.AddManagementAsync(management.Value, ct);
        return Result<bool>.Success(true);
    }
}

public sealed class RemoveDepartmentManagerHandler(AccountAccess access, IAccountRepository accounts)
    : ICommandHandler<RemoveDepartmentManagerCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(RemoveDepartmentManagerCommand command, CancellationToken ct = default)
    {
        var loaded = await DepartmentLinks.LoadAsync(access, accounts, command.ActorId, command.DepartmentId, command.EmployeeId, ct);
        if (loaded.IsFailure) return Result<bool>.Failure(loaded.Errors.ToArray());
        var (department, employee) = loaded.Value;

        var management = await accounts.GetManagementAsync(department.Id, employee.Id, ct);
        if (management is null)
            return Result<bool>.Failure(new Error("DepartmentManagementNotFound", "The employee does not manage the department."));

        accounts.RemoveManagement(management);
        return Result<bool>.Success(true);
    }
}

internal static class DepartmentLinks
{
    public static async Task<Result<(Department Department, Employee Employee)>> LoadAsync(
        AccountAccess access, IAccountRepository accounts, Guid actorId, Guid departmentId, Guid employeeId, CancellationToken ct)
    {
        var admin = await access.RequireAdministratorAsync(actorId, ct);
        if (admin.IsFailure) return Result<(Department, Employee)>.Failure(admin.Errors.ToArray());

        var ids = AccountInput.Link(departmentId, employeeId);
        if (ids.IsFailure) return Result<(Department, Employee)>.Failure(ids.Errors.ToArray());

        var department = await accounts.GetDepartmentAsync(ids.Value.Department, ct);
        if (department is null) return Result<(Department, Employee)>.Failure(AccountInput.DepartmentNotFound);

        var employee = await accounts.GetEmployeeAsync(ids.Value.Employee, ct);
        if (employee is null) return Result<(Department, Employee)>.Failure(AccountInput.EmployeeNotFound);

        return Result<(Department, Employee)>.Success((department, employee));
    }
}
