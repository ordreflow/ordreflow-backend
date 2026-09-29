namespace Domain.Common.IUnitOfWork;

public interface IUnitOfWork
{
    
    //TODO: 
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

