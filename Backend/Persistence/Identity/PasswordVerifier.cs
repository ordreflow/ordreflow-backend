namespace Persistence.Identity;

using Application.Interfaces;
using Microsoft.AspNetCore.Identity;

public class PasswordVerifier(UserManager<ApplicationUser> userManager) : IPasswordVerifier
{
    public async Task<Guid?> VerifyAsync(string email, string password)
    {
        var account = await userManager.FindByEmailAsync(email);
        if (account is null || await userManager.IsLockedOutAsync(account))
            return null;

        if (!await userManager.CheckPasswordAsync(account, password))
        {
            // Counts towards Identity's lockout after repeated failed attempts.
            await userManager.AccessFailedAsync(account);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(account);
        return account.EmployeeId == Guid.Empty ? null : account.EmployeeId;
    }
}
