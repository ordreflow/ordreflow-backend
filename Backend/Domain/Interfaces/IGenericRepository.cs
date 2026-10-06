namespace Domain.Interfaces;

public interface IGenericRepository<T, TId>
{
    Task<T?> GetAsync(TId id);

    Task AddAsync(T entity);

    Task RemoveAsync(TId id);
}