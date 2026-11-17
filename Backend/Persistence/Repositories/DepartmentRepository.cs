namespace Persistence.Repositories;

using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public class DepartmentRepository(AppDbContext context)
    : GenericRepository<Department, DepartmentId>(context),
        IDepartmentRepository
{
    public override async Task<Department?> GetAsync(DepartmentId id)
    {
        return await Context.Departments
            .Include(department => department.Members)
            .SingleOrDefaultAsync(department => department.Id == id);
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync()
    {
        return await Context.Departments
            .Include(department => department.Members)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Department>> GetByMemberAsync(UserId userId)
    {
        return await Context.Departments
            .Include(department => department.Members)
            .Where(department => department.Members.Any(member => member.UserId == userId))
            .ToListAsync();
    }

    public async Task<bool> NameExistsAsync(DepartmentName name, DepartmentId? exceptId = null)
    {
        return await Context.Departments
            .AnyAsync(department => department.Name == name && (exceptId == null || department.Id != exceptId));
    }
}
