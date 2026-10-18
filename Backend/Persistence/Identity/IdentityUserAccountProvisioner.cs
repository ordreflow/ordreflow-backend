namespace Persistence.Identity;

using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

public class IdentityUserAccountProvisioner(UserManager<ApplicationUser> userManager, AppDbContext context)
    : IUserAccountProvisioner
{
    public async Task<Result> CreateAsync(
        UserId userId,
        EmailAddress email,
        string password,
        UserRole role)
    {
        var account = new ApplicationUser
        {
            UserId = userId,
            UserName = email.Value,
            Email = email.Value
        };

        // One transaction, so a failed role assignment rolls back the AspNetUsers row too.
        await using var transaction = await context.Database.BeginTransactionAsync();

        var createResult = await userManager.CreateAsync(account, password);
        if (!createResult.Succeeded)
            return Result.Failure(createResult.Errors
                .Select(error => new Error(error.Code, error.Description))
                .ToArray());

        var roleResult = await userManager.AddToRoleAsync(account, role.ToString());
        if (!roleResult.Succeeded)
            return Result.Failure(roleResult.Errors
                .Select(error => new Error(error.Code, error.Description))
                .ToArray());

        await transaction.CommitAsync();
        return Result.Success();
    }
}
