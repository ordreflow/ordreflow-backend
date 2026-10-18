using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IPasswordVerifier
{
    // Returns the id of the user the email and password belong to, or null if they do not match.
    Task<UserId?> VerifyAsync(EmailAddress email, string password);
}
