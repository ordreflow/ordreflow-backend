using Application.Exports;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface IExportRepository
{
    Task<bool> EmployeeExistsAsync(EmployeeId employee, CancellationToken ct);
    Task<IReadOnlyList<ExportCandidate>> SelectApprovedAsync(DateOnly from, DateOnly to, EmployeeId? employee, CancellationToken ct);
    Task<Export?> GetAsync(ExportId id, CancellationToken ct);
    Task<IReadOnlyList<ExportSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<ExportEmployeeDto>> EmployeesAsync(string? search, int page, int pageSize, CancellationToken ct);
    // These writes own their transactions; the legacy command dispatcher cannot return a created batch.
    Task<Result<Export>> SaveNewAsync(Export batch, CancellationToken ct);
    Task<Result> CloseTimeEntriesAsync(ExportId id, EmployeeId actor, DateTime closedAtUtc, CancellationToken ct);
}
