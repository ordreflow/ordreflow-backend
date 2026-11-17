using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IDepartmentRepository : IGenericRepository<Department, DepartmentId>
{
    Task<IReadOnlyList<Department>> GetAllAsync();

    Task<IReadOnlyList<Department>> GetByMemberAsync(UserId userId);

    Task<bool> NameExistsAsync(DepartmentName name, DepartmentId? exceptId = null);
}
