using Application;
using Application.Commands;
using Application.Exports;
using Application.Extenstions;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiTests.IntegrationTest.Fixtures;

// Exercises actual command/query registration and dispatch; only storage/transaction
// dependencies are replaceable. Repository tests supply the real transaction boundary.
internal sealed class ExportTestHarness
{
    private readonly ServiceProvider _services;
    private readonly IExportRepository _repository;
    public ExportTestHarness(IEmployeeAccessRepository employees, IExportRepository repository, TimeProvider clock,
        ICommandTransaction? transactions = null)
    {
        _repository = repository;
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(employees);
        services.AddSingleton(repository);
        services.AddSingleton(clock);
        services.AddSingleton<IUnitOfWork>(new NoopWork());
        services.AddSingleton<ICommandTransaction>(transactions ?? new InMemoryBoundary());
        _services = services.BuildServiceProvider();
    }

    public Task<Result<ExportDetailsDto>> CreateAsync(Guid actor, CreateExportCommand request) =>
        _services.GetRequiredService<ICommandDispatcher>().DispatchAsync<CreateExportCommand, ExportDetailsDto>(request with { RequestedBy = actor });
    public async Task<Result> CloseTimeEntriesAsync(Guid actor, Guid id) =>
        await _services.GetRequiredService<ICommandDispatcher>().DispatchAsync<CloseExportTimeEntriesCommand, bool>(new(actor, id));
    public async Task<Result<Export>> GetAsync(Guid actor, Guid id)
    {
        var details = await _services.GetRequiredService<IQueryDispatcher>().DispatchAsync<GetExportQuery, Result<ExportDetailsDto>>(new(actor, id));
        return details.IsFailure ? Result<Export>.Failure(details.Errors.ToArray())
            : Result<Export>.Success((await _repository.GetAsync(Domain.ValueObjects.ExportId.Create(id).Value, default))!);
    }
    public Task<Result<IReadOnlyList<ExportSummaryDto>>> ListAsync(Guid actor, int page, int size) =>
        _services.GetRequiredService<IQueryDispatcher>().DispatchAsync<ListExportsQuery, Result<IReadOnlyList<ExportSummaryDto>>>(new(actor, page, size));
    public Task<Result<IReadOnlyList<ExportEmployeeDto>>> EmployeesAsync(Guid actor, string? search, int page, int size) =>
        _services.GetRequiredService<IQueryDispatcher>().DispatchAsync<ExportEmployeesQuery, Result<IReadOnlyList<ExportEmployeeDto>>>(new(actor, search, page, size));

    private sealed class NoopWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class InMemoryBoundary : ICommandTransaction
    {
        public Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, string conflictCode,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
}
