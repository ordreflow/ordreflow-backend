using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IEmployeeAccessRepository
{
    Task<Employee?> GetWithAccessesAsync(EmployeeId employeeId, CancellationToken cancellationToken = default);
}
