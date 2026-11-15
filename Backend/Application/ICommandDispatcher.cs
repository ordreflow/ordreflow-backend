using   Core.Tools.OperationResult;

namespace Application;

public interface ICommandDispatcher
{
    Task<Result> DispatchAsync<TCommand>(TCommand command);
    Task<Result<TValue>> DispatchAsync<TCommand, TValue>(TCommand command, CancellationToken cancellationToken = default);
}
