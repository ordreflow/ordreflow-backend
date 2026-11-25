using System.Data;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Persistence;

public sealed class CommandTransaction(AppDbContext context) : ICommandTransaction
{
    public async Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation,
        string conflictCode, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                if (result.IsFailure)
                {
                    context.ChangeTracker.Clear();
                    return result;
                }
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception error) when (IsRetryable(error))
            {
                // Also safe if PostgreSQL already aborted the transaction at commit.
                await transaction.DisposeAsync();
                context.ChangeTracker.Clear();
            }
            catch
            {
                context.ChangeTracker.Clear();
                throw;
            }
        }
        return Result<T>.Failure(new Error(conflictCode, "The data changed concurrently; refresh and retry the same operation."));
    }

    private static bool IsRetryable(Exception error) => error is DbUpdateConcurrencyException ||
        error is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } ||
        error is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation } };
}
