using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces.IUnitOfWork;

namespace WebApiTests.IntegrationTest;

public sealed class ReviewDecisionCommitTests
{
    [Fact]
    public async Task Concurrent_save_is_reported_as_a_conflict_instead_of_success()
    {
        var dispatcher = new UnitOfWorkCommandDispatcherDecorator(
            new SuccessfulDispatcher(), new ConflictingUnitOfWork());

        var result = await dispatcher.DispatchAsync(new ApproveTimeEntryCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal("TimeEntryConflict", Assert.Single(result.Errors).Code);
    }

    private sealed class SuccessfulDispatcher : ICommandDispatcher
    {
        public Task<Result> DispatchAsync<TCommand>(TCommand command) => Task.FromResult(Result.Success());
    }

    private sealed class ConflictingUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new ConcurrentTimeEntryException();
    }
}
