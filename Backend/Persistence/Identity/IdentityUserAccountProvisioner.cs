namespace Persistence.Identity;

using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class IdentityUserAccountProvisioner(UserManager<ApplicationUser> userManager, AppDbContext context)
    : IEmployeeAccountProvisioner
{
    public async Task<Result> CreateAsync(
        Guid employeeId,
        string email,
        string password)
    {
        var typedEmployeeId = EmployeeId.Create(employeeId);
        if (typedEmployeeId.IsFailure || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result.Failure(new Error("InvalidAccount", "A valid employee, email and password are required."));

        var employeeKey = EmployeeId.Create(employeeId).Value;
        var employee = context.Employees.Local.FirstOrDefault(value => value.Id == employeeKey)
            ?? await context.Employees.Include(value => value.Accesses)
                .SingleOrDefaultAsync(value => value.Id == employeeKey);
        if (employee is null || !employee.HasPermission(SystemPermission.Employee))
            return Result.Failure(new Error("EmployeeAccessForbidden", "An active employee account is required."));
        if (!string.Equals(employee.Email.Value, email, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(new Error("InvalidAccount", "The login email must match the employee email."));

        var account = new ApplicationUser
        {
            EmployeeId = employeeKey,
            UserName = email,
            Email = email
        };

        // Identity's save also flushes a newly tracked employee and its baseline access.
        await using var transaction = await context.Database.BeginTransactionAsync();

        var createResult = await userManager.CreateAsync(account, password);
        if (!createResult.Succeeded)
            return Result.Failure(createResult.Errors
                .Select(error => new Error(error.Code, error.Description))
                .ToArray());

        await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> SetPasswordAsync(Guid employeeId, string password)
    {
        var typedEmployeeId = EmployeeId.Create(employeeId);
        if (typedEmployeeId.IsFailure || string.IsNullOrWhiteSpace(password))
            return Result.Failure(new Error("InvalidAccount", "A valid employee and password are required."));

        var employeeKey = typedEmployeeId.Value;
        var account = await userManager.Users.SingleOrDefaultAsync(user => user.EmployeeId == employeeKey);
        if (account is null)
            return Result.Failure(new Error("LoginNotFound", "The employee has no login account."));

        // Validate before touching the hash, so a rejected password leaves the old one working.
        var errors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, account, password);
            if (!validation.Succeeded) errors.AddRange(validation.Errors);
        }
        if (errors.Count > 0)
            return Result.Failure(errors.Select(error => new Error(error.Code, error.Description)).ToArray());

        // Hash, unlock and rotate the security stamp in one save.
        account.PasswordHash = userManager.PasswordHasher.HashPassword(account, password);
        account.AccessFailedCount = 0;
        account.LockoutEnd = null;
        var updated = await userManager.UpdateSecurityStampAsync(account);
        return updated.Succeeded
            ? Result.Success()
            : Result.Failure(updated.Errors.Select(error => new Error(error.Code, error.Description)).ToArray());
    }
}
