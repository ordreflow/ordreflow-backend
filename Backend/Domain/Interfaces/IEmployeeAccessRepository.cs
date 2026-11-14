using Domain.Aggregate;

namespace Domain.Interfaces;

public interface IEmployeeAccessRepository
{
    Task<Employee?> GetWithAccessesAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
