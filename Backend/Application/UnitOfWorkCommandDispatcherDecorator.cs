using System.Threading.Tasks;
using Core.Tools.OperationResult;
using Domain.Interfaces.IUnitOfWork;

namespace Application;

// Wraps the inner dispatcher and automatically handles saving changes to the database.
public class UnitOfWorkCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _innerDispatcher;
    private readonly IUnitOfWork _unitOfWork;

    // We inject the inner ICommandDispatcher (the real dispatcher or another decorator)
    // and the IUnitOfWork to commit our transactions.
    public UnitOfWorkCommandDispatcherDecorator(ICommandDispatcher innerDispatcher, IUnitOfWork unitOfWork)
    {
        _innerDispatcher = innerDispatcher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> DispatchAsync<TCommand>(TCommand command)
    {
        // 1. Delegate the actual work to the inner dispatcher/handler
        var result = await _innerDispatcher.DispatchAsync(command);

        // 2. If the operation failed, we don't save any changes
        if (!result.IsSuccess)
        {
            return result;
        }

        // 3. If everything went well, save all changes made by the handler at once
        await _unitOfWork.SaveChangesAsync();

        return result;
    }
}

