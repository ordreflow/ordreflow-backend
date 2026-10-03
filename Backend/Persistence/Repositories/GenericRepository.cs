using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;


public class GenericRepository<T, TId>(
    AppDbContext context)
    : IGenericRepository<T, TId>
    where T : class
{
    protected readonly AppDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetAsync(TId id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public async Task RemoveAsync(TId id)
    {
        var entity = await GetAsync(id);

        if (entity is not null)
        {
            DbSet.Remove(entity);
        }
    }
}