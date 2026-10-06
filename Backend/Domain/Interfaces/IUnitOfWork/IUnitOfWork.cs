namespace Domain.Interfaces.IUnitOfWork;

public interface IUnitOfWork
{
    
    //TODO: 
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

