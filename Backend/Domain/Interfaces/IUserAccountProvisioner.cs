using Domain.ValueObjects;

namespace Domain.Interfaces;

using Core.Tools.OperationResult;

public interface IUserAccountProvisioner
{
    // Creates the login credential (password + role) for an already-created domain user.
    Task<Result> CreateAsync(UserId userId, EmailAddress email, string password, UserRole role);
}
