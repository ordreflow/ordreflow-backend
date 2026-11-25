namespace Application.Interfaces;

public interface IPasswordVerifier
{
    Task<Guid?> VerifyAsync(string email, string password);
}
