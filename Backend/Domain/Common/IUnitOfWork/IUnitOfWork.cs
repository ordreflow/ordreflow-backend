namespace Domain.Common.IUnitOfWork;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

