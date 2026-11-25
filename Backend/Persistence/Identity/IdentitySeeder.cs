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

        if (!System.Net.Mail.MailAddress.TryCreate(adminEmail, out var address))
            throw new InvalidOperationException("ADMIN_EMAIL is not a valid email address.");

        var email = address.Address;
        var name = string.IsNullOrWhiteSpace(adminName) ? "Administrator" : adminName.Trim();

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        await using var transaction = await context.Database.BeginTransactionAsync();

        var emailAddress = EmailAddress.Create(email).Value;
        var employee = await context.Employees.Include(value => value.Accesses)
            .SingleOrDefaultAsync(value => value.Email == emailAddress);
        if (employee is null)
        {
            var created = Employee.Create(name, email, DateTime.UtcNow);
            if (created.IsFailure)
                throw new InvalidOperationException("The admin employee could not be created.");
            employee = created.Value;
            if (employee.GrantPermission(SystemPermission.Administrator).IsFailure)
                throw new InvalidOperationException("The admin permission could not be granted.");
            context.Employees.Add(employee);
        }
        else if (!employee.HasPermission(SystemPermission.Employee) ||
                 !employee.HasPermission(SystemPermission.Administrator))
        {
            throw new InvalidOperationException("ADMIN_EMAIL belongs to an employee without administrator access.");
        }

        var account = new ApplicationUser
        {
            EmployeeId = employee.Id,
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        EnsureSucceeded(await userManager.CreateAsync(account, adminPassword));

        await transaction.CommitAsync();
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Admin seed failed: " + string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}
