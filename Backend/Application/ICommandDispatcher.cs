using   Core.Tools.OperationResult;
using   Core.Tools.OperationResult;

namespace Application;

public interface ICommandDispatcher
{
    Task<Result> DispatchAsync<TCommand>(TCommand command);
}