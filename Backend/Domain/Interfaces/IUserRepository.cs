using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IUserRepository : IGenericRepository<User, UserId>
{
    Task<User?> GetByEmailAsync(EmailAddress email);

    Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId);
}