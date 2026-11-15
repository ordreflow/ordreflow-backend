using System.Threading.Tasks;
using Core.Tools.OperationResult;
using Domain.Interfaces.IUnitOfWork;
using Application.Interfaces;

namespace Application;

// Wraps the inner dispatcher and automatically handles saving changes to the database.
public class UnitOfWorkCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _innerDispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommandTransaction? _transactions;

    // We inject the inner ICommandDispatcher (the real dispatcher or another decorator)
    // and the IUnitOfWork to commit our transactions.
    public UnitOfWorkCommandDispatcherDecorator(ICommandDispatcher innerDispatcher, IUnitOfWork unitOfWork, ICommandTransaction? transactions = null)
    {
        _innerDispatcher = innerDispatcher;
        _unitOfWork = unitOfWork;
        _transactions = transactions;
    }

    public async Task<Result<TValue>> DispatchAsync<TCommand, TValue>(TCommand command, CancellationToken cancellationToken = default)
    {
        if (command is ITransactionalCommand transactional)
        {
            var boundary = _transactions ?? throw new InvalidOperationException("Transactional commands require ICommandTransaction.");
            return await boundary.ExecuteAsync(ct => _innerDispatcher.DispatchAsync<TCommand, TValue>(command, ct),
                transactional.ConflictCode, cancellationToken);
        }
        var result = await _innerDispatcher.DispatchAsync<TCommand, TValue>(command, cancellationToken);
        if (result.IsFailure) return result;
        try { await _unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (ConcurrentTimeEntryException)
        { return Result<TValue>.Failure(new Error("TimeEntryConflict", "The time entry changed; refresh and retry.")); }
        return result;
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
        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (ConcurrentTimeEntryException)
        {
            return Result.Failure(new Error("TimeEntryConflict", "The time entry changed; refresh and retry."));
        }

        return result;
    }
}
