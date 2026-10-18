namespace Persistence.Identity;

using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

public class PasswordVerifier(UserManager<ApplicationUser> userManager) : IPasswordVerifier
{
    public async Task<UserId?> VerifyAsync(EmailAddress email, string password)
    {
        var account = await userManager.FindByEmailAsync(email.Value);
        if (account is null || await userManager.IsLockedOutAsync(account))
            return null;

        if (!await userManager.CheckPasswordAsync(account, password))
        {
            // Counts towards Identity's lockout after repeated failed attempts.
            await userManager.AccessFailedAsync(account);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(account);
        return account.UserId;
    }
}
