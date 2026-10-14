namespace Domain.Interfaces.IUnitOfWork;

public interface IUnitOfWork
{
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

