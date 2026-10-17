namespace Persistence.Identity;

using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class IdentitySeeder
{
    // Creates the first admin login if it does not exist yet. Skipped when email or password is missing.
    public static async Task SeedAdminAsync(
        IServiceProvider services,
        string? adminEmail,
        string? adminPassword,
        string? adminName)
    {
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var email = EmailAddress.Create(adminEmail);
        if (email.IsFailure)
            throw new InvalidOperationException("ADMIN_EMAIL is not a valid email address.");

        var name = PersonName.Create(string.IsNullOrWhiteSpace(adminName) ? "Administrator" : adminName);
        if (name.IsFailure)
            throw new InvalidOperationException("ADMIN_NAME is not a valid name.");

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(email.Value.Value) is not null)
            return;

        await using var transaction = await context.Database.BeginTransactionAsync();

        var user = await context.Users.FirstOrDefaultAsync(x => x.Email == email.Value);
        if (user is null)
        {
            user = User.Create(name.Value, email.Value, UserRole.Admin).Value;
            context.Users.Add(user);
        }
        else if (user.Role != UserRole.Admin)
        {
            throw new InvalidOperationException("ADMIN_EMAIL belongs to an existing user who is not an admin.");
        }

        var account = new ApplicationUser
        {
            UserId = user.UserId,
            UserName = email.Value.Value,
            Email = email.Value.Value,
            EmailConfirmed = true
        };

        EnsureSucceeded(await userManager.CreateAsync(account, adminPassword));
        EnsureSucceeded(await userManager.AddToRoleAsync(account, IdentityRoles.Admin));

        await transaction.CommitAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Admin seed failed: " + string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}
