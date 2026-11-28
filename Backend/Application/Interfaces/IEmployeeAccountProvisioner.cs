using Core.Tools.OperationResult;

namespace Application.Interfaces;

public interface IEmployeeAccountProvisioner
{
    Task<Result> CreateAsync(Guid employeeId, string email, string password);

    /// <summary>Replaces the login password and clears any lockout. The old password is not needed.</summary>
    Task<Result> SetPasswordAsync(Guid employeeId, string password);
}
