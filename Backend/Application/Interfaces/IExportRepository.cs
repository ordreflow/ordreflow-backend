using Application.Exports;
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
    Task AddAsync(Export batch, CancellationToken ct);
    Task<Export?> GetForClosureAsync(ExportId id, CancellationToken ct);
    Task<IReadOnlyList<TimeEntry>> GetEntriesForClosureAsync(IReadOnlyCollection<TimeEntryId> ids, CancellationToken ct);
}
