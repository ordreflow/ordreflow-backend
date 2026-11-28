using Application.Accounts;
using Application.Commands;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateEmployeeHandler(
    AccountAccess access,
    IAccountRepository accounts,
    IEmployeeAccountProvisioner provisioner,
    TimeProvider clock) : ICommandHandler<CreateEmployeeCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateEmployeeCommand command, CancellationToken ct = default)
    {
        var admin = await access.RequireAdministratorAsync(command.ActorId, ct);
        if (admin.IsFailure) return Result<Guid>.Failure(admin.Errors.ToArray());

        var title = AccountInput.Title(command.Title);
        if (title.IsFailure) return Result<Guid>.Failure(title.Errors.ToArray());
        var permissions = AccountInput.Permissions(command.Permissions);
        if (permissions.IsFailure) return Result<Guid>.Failure(permissions.Errors.ToArray());
        if (string.IsNullOrWhiteSpace(command.Password))
            return Result<Guid>.Failure(new Error("PasswordRequired", "A password is required."));

        var created = Employee.Create(command.Name, command.Email, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure) return Result<Guid>.Failure(created.Errors.ToArray());
        var employee = created.Value;

        if (await accounts.EmailInUseAsync(employee.Email, ct))
            return Result<Guid>.Failure(new Error("EmailAlreadyInUse", "An employee with this email already exists."));

        var profile = employee.ChangeProfile(employee.Name, employee.Email, title.Value);
        if (profile.IsFailure) return Result<Guid>.Failure(profile.Errors.ToArray());
        foreach (var permission in permissions.Value)
        {
            var granted = employee.GrantPermission(permission);
            if (granted.IsFailure) return Result<Guid>.Failure(granted.Errors.ToArray());
        }

        // The employee must be tracked first: creating the login saves both in one transaction.
        await accounts.AddEmployeeAsync(employee, ct);
        var login = await provisioner.CreateAsync(employee.Id.Value, employee.Email.Value, command.Password);
        return login.IsFailure
            ? Result<Guid>.Failure(login.Errors.ToArray())
            : Result<Guid>.Success(employee.Id.Value);
    }
}

public sealed class UpdateEmployeeProfileHandler(AccountAccess access, IAccountRepository accounts)
    : ICommandHandler<UpdateEmployeeProfileCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(UpdateEmployeeProfileCommand command, CancellationToken ct = default)
    {
        var admin = await access.RequireAdministratorAsync(command.ActorId, ct);
        if (admin.IsFailure) return Result<bool>.Failure(admin.Errors.ToArray());

        var name = PersonName.Create(command.Name);
        if (name.IsFailure) return Result<bool>.Failure(name.Errors.ToArray());
        var title = AccountInput.Title(command.Title);
        if (title.IsFailure) return Result<bool>.Failure(title.Errors.ToArray());

        var employee = await EmployeeCommands.FindAsync(accounts, command.EmployeeId, ct);
        if (employee.IsFailure) return Result<bool>.Failure(employee.Errors.ToArray());

        // The login email is tied to the Identity account, so it is not changed here.
        var changed = employee.Value.ChangeProfile(name.Value, employee.Value.Email, title.Value);
        return changed.IsFailure ? Result<bool>.Failure(changed.Errors.ToArray()) : Result<bool>.Success(true);
    }
}

public sealed class SetEmployeePermissionsHandler(AccountAccess access, IAccountRepository accounts)
    : ICommandHandler<SetEmployeePermissionsCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(SetEmployeePermissionsCommand command, CancellationToken ct = default)
    {
        var admin = await access.RequireAdministratorAsync(command.ActorId, ct);
        if (admin.IsFailure) return Result<bool>.Failure(admin.Errors.ToArray());

        var permissions = AccountInput.Permissions(command.Permissions);
        if (permissions.IsFailure) return Result<bool>.Failure(permissions.Errors.ToArray());

        var employee = await EmployeeCommands.FindAsync(accounts, command.EmployeeId, ct);
        if (employee.IsFailure) return Result<bool>.Failure(employee.Errors.ToArray());
        var target = employee.Value;

        // An administrator cannot lock themselves out of administration.
        if (target.Id == admin.Value.Id &&
            !(permissions.Value.Contains(SystemPermission.Administrator) && permissions.Value.Contains(SystemPermission.Employee)))
            return Result<bool>.Failure(new Error("SelfAdministrationRemovalForbidden",
                "You cannot remove your own administrator or employee access."));

        foreach (var current in target.Accesses.Select(access => access.Permission).ToArray())
        {
            if (!permissions.Value.Contains(current))
            {
                var revoked = target.RevokePermission(current);
                if (revoked.IsFailure) return Result<bool>.Failure(revoked.Errors.ToArray());
            }
        }

        foreach (var permission in permissions.Value)
        {
            var granted = target.GrantPermission(permission);
            if (granted.IsFailure) return Result<bool>.Failure(granted.Errors.ToArray());
        }

        return Result<bool>.Success(true);
    }
}

public sealed class SetEmployeePasswordHandler(
    AccountAccess access,
    IAccountRepository accounts,
    IEmployeeAccountProvisioner provisioner) : ICommandHandler<SetEmployeePasswordCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(SetEmployeePasswordCommand command, CancellationToken ct = default)
    {
        var admin = await access.RequireAdministratorAsync(command.ActorId, ct);
        if (admin.IsFailure) return Result<bool>.Failure(admin.Errors.ToArray());
        if (string.IsNullOrWhiteSpace(command.Password))
            return Result<bool>.Failure(new Error("PasswordRequired", "A password is required."));

        var employee = await EmployeeCommands.FindAsync(accounts, command.EmployeeId, ct);
        if (employee.IsFailure) return Result<bool>.Failure(employee.Errors.ToArray());

        var changed = await provisioner.SetPasswordAsync(employee.Value.Id.Value, command.Password);
        return changed.IsFailure ? Result<bool>.Failure(changed.Errors.ToArray()) : Result<bool>.Success(true);
    }
}

public sealed class DeactivateEmployeeHandler(AccountAccess access, IAccountRepository accounts)
    : ICommandHandler<DeactivateEmployeeCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(DeactivateEmployeeCommand command, CancellationToken ct = default)
    {
        var admin = await access.RequireAdministratorAsync(command.ActorId, ct);
        if (admin.IsFailure) return Result<bool>.Failure(admin.Errors.ToArray());

        var employee = await EmployeeCommands.FindAsync(accounts, command.EmployeeId, ct);
        if (employee.IsFailure) return Result<bool>.Failure(employee.Errors.ToArray());

        if (employee.Value.Id == admin.Value.Id)
            return Result<bool>.Failure(new Error("SelfDeactivationForbidden", "You cannot deactivate your own account."));
        if (employee.Value.Status == EmployeeStatus.Deactivated)
            return Result<bool>.Failure(new Error("EmployeeAlreadyDeactivated", "The employee is already deactivated."));

        // The employee and their time entries are kept; only sign-in and access stop (FR-05).
        employee.Value.Deactivate();
        return Result<bool>.Success(true);
    }
}

public sealed class ActivateEmployeeHandler(AccountAccess access, IAccountRepository accounts)
    : ICommandHandler<ActivateEmployeeCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(ActivateEmployeeCommand command, CancellationToken ct = default)
    {
        var admin = await access.RequireAdministratorAsync(command.ActorId, ct);
        if (admin.IsFailure) return Result<bool>.Failure(admin.Errors.ToArray());

        var employee = await EmployeeCommands.FindAsync(accounts, command.EmployeeId, ct);
        if (employee.IsFailure) return Result<bool>.Failure(employee.Errors.ToArray());
        if (employee.Value.Status == EmployeeStatus.Active)
            return Result<bool>.Failure(new Error("EmployeeAlreadyActive", "The employee is already active."));

        employee.Value.Activate();
        return Result<bool>.Success(true);
    }
}

internal static class EmployeeCommands
{
    public static async Task<Result<Employee>> FindAsync(IAccountRepository accounts, Guid employeeId, CancellationToken ct)
    {
        var id = EmployeeId.Create(employeeId);
        if (id.IsFailure) return Result<Employee>.Failure(id.Errors.ToArray());

        var employee = await accounts.GetEmployeeAsync(id.Value, ct);
        return employee is null
            ? Result<Employee>.Failure(AccountInput.EmployeeNotFound)
            : Result<Employee>.Success(employee);
    }
}
