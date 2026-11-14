using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class EmployeeAccessRepository(AppDbContext context) : IEmployeeAccessRepository
{
    public Task<Employee?> GetWithAccessesAsync(EmployeeId employeeId, CancellationToken cancellationToken = default) =>
        context.Employees
            .AsNoTracking()
            .Include(employee => employee.Accesses)
            .SingleOrDefaultAsync(employee => employee.Id == employeeId, cancellationToken);
}
