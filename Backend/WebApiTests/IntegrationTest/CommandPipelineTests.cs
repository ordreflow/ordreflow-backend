using Application;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Interfaces.IUnitOfWork;

namespace WebApiTests.IntegrationTest;

public sealed class CommandPipelineTests
{
    [Fact]
    public async Task Transactional_command_uses_one_boundary_and_does_not_commit_again_in_the_decorator()
    {
        var inner = new ValueDispatcher();
        var work = new CountingWork();
        var transaction = new CountingTransaction();
        var dispatcher = new UnitOfWorkCommandDispatcherDecorator(inner, work, transaction);

        var result = await dispatcher.DispatchAsync<AtomicCommand, bool>(new());

        Assert.True(result.Value);
        Assert.Equal(1, transaction.Executions);
        Assert.Equal(1, inner.Executions);
        Assert.Equal(0, work.Saves);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Ordinary_result_commands_commit_once_only_on_success(bool success, int saves)
    {
        var work = new CountingWork();
        var dispatcher = new UnitOfWorkCommandDispatcherDecorator(new ValueDispatcher(success), work);

        var result = await dispatcher.DispatchAsync<string, bool>("ordinary");

        Assert.Equal(success, result.IsSuccess);
        Assert.Equal(saves, work.Saves);
    }

    private sealed record AtomicCommand : ITransactionalCommand
    {
        public string ConflictCode => "ExportClosureConflict";
    }
    private sealed class CountingTransaction : ICommandTransaction
    {
        public int Executions { get; private set; }
        public Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, string code,
            CancellationToken cancellationToken = default)
        {
            Executions++;
            return operation(cancellationToken);
        }
    }
    private sealed class CountingWork : IUnitOfWork
    {
        public int Saves { get; private set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.CompletedTask; }
    }
    private sealed class ValueDispatcher(bool success = true) : ICommandDispatcher
    {
        public int Executions { get; private set; }
        public Task<Result> DispatchAsync<T>(T command) => throw new NotSupportedException();
        public Task<Result<T>> DispatchAsync<TCommand, T>(TCommand command, CancellationToken cancellationToken = default)
        {
            Executions++;
            return Task.FromResult(success ? Result<T>.Success((T)(object)true)
                : Result<T>.Failure(new Error("Rejected", "The command failed.")));
        }
    }
}
