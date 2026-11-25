using Core.Tools.OperationResult;

namespace Application.Interfaces;

// The application chooses the boundary; infrastructure owns isolation, commit, and retry mechanics.
public interface ICommandTransaction
{
    Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation,
        string conflictCode, CancellationToken cancellationToken = default);
}
