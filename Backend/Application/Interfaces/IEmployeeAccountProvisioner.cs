using Core.Tools.OperationResult;

namespace Application.Interfaces;

public interface IEmployeeAccountProvisioner
{
    Task<Result> CreateAsync(Guid employeeId, string email, string password);
}
