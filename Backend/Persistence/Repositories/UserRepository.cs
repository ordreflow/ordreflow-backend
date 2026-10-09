using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class UserRepository(AppDbContext context)
    : GenericRepository<User, UserId>(context),
        IUserRepository
{
    public async Task<User?> GetByEmailAsync(
        EmailAddress email)
    {
        return await Context.Users
            .FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<IReadOnlyList<User>> GetByManagerIdAsync(
        UserId managerId)
    {
        return await Context.Users
            .Where(user => user.ManagerId == managerId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        return await Context.Users.ToListAsync();
    }
}