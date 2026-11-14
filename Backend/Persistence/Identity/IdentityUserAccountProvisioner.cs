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

        var employee = context.Employees.Local.FirstOrDefault(value => value.Id == typedEmployeeId.Value)
            ?? await context.Employees.Include(value => value.Accesses)
                .SingleOrDefaultAsync(value => value.Id == typedEmployeeId.Value);
        if (employee is null || !employee.HasPermission(SystemPermission.Employee))
            return Result.Failure(new Error("EmployeeAccessForbidden", "An active employee account is required."));
        if (!string.Equals(employee.Email, email, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(new Error("InvalidAccount", "The login email must match the employee email."));

        var account = new ApplicationUser
        {
            EmployeeId = typedEmployeeId.Value,
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
}
