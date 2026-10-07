using Core.Tools.OperationResult;

namespace Application;

public interface IQueryDispatcher
{
    Task<Result<TResult>> DispatchAsync<TQuery, TResult>(
        TQuery query);
}