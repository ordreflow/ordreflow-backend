using   Core.Tools.OperationResult;

namespace Application;

public interface ICommandHandler<TCommand>
{
    Task<Result> HandleAsync(TCommand command);
    
}