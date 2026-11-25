using   Core.Tools.OperationResult;

namespace Application;

public interface ICommandHandler<TCommand>
{
    Task<Result> HandleAsync(TCommand command);
    
}

public interface ICommandHandler<TCommand, TValue>
{
    Task<Result<TValue>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
