using Application.Interfaces;
using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Exports;

public sealed class PayrollExportService(IEmployeeAccessRepository employees, IExportRepository exports, TimeProvider clock)
{
    public async Task<Result<ExportDetailsDto>> CreateAsync(Guid actorId, CreateExportCommand request, CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(actorId, ct);
        if (access.IsFailure) return Result<ExportDetailsDto>.Failure(access.Errors.ToArray());
        var id = ExportId.Create(request.ExportId);
        if (id.IsFailure) return Result<ExportDetailsDto>.Failure(id.Errors.ToArray());
        if (request.FromDate == default || request.ToDate < request.FromDate)
            return Result<ExportDetailsDto>.Failure(new Error("InvalidDateRange", "Select a valid inclusive date range."));
        EmployeeId? employee = null;
        if (request.EmployeeId is { } employeeId)
        {
            var typed = EmployeeId.Create(employeeId);
            if (typed.IsFailure) return Result<ExportDetailsDto>.Failure(typed.Errors.ToArray());
            employee = typed.Value;
        }
        var existing = await exports.GetAsync(id.Value, ct);
        if (existing is not null)
            return existing.MatchesRequest(access.Value, request.FromDate, request.ToDate, employee)
                ? Result<ExportDetailsDto>.Success(Details(existing))
                : Result<ExportDetailsDto>.Failure(new Error("ExportRequestConflict", "This export ID was used for a different request."));
        if (employee is not null && !await exports.EmployeeExistsAsync(employee, ct))
            return Result<ExportDetailsDto>.Failure(new Error("EmployeeNotFound", "The selected employee was not found."));
        var selected = await exports.SelectApprovedAsync(request.FromDate, request.ToDate, employee, ct);
        var batch = Export.Create(id.Value, access.Value, request.FromDate, request.ToDate, employee,
            selected.Select(item => item.Entry).ToArray(), ExportCsv.Create(selected), clock.GetUtcNow().UtcDateTime);
        if (batch.IsFailure) return Result<ExportDetailsDto>.Failure(batch.Errors.ToArray());
        var saved = await exports.SaveNewAsync(batch.Value, ct);
        return saved.IsFailure ? Result<ExportDetailsDto>.Failure(saved.Errors.ToArray())
            : Result<ExportDetailsDto>.Success(Details(saved.Value));
    }

    public async Task<Result<Export>> GetAsync(Guid actorId, Guid id, CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(actorId, ct);
        if (access.IsFailure) return Result<Export>.Failure(access.Errors.ToArray());
        var typed = ExportId.Create(id);
        if (typed.IsFailure) return Result<Export>.Failure(typed.Errors.ToArray());
        var batch = await exports.GetAsync(typed.Value, ct);
        return batch is null ? Result<Export>.Failure(new Error("ExportNotFound", "The export was not found."))
            : Result<Export>.Success(batch);
    }

    public async Task<Result<IReadOnlyList<ExportSummaryDto>>> ListAsync(Guid actorId, int page, int pageSize, CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(actorId, ct);
        if (access.IsFailure) return Result<IReadOnlyList<ExportSummaryDto>>.Failure(access.Errors.ToArray());
        if (!ValidPage(page, pageSize)) return Result<IReadOnlyList<ExportSummaryDto>>.Failure(new Error("InvalidPage", "Use page >= 1 and pageSize 1–100."));
        return Result<IReadOnlyList<ExportSummaryDto>>.Success(await exports.ListAsync(page, pageSize, ct));
    }

    public async Task<Result<IReadOnlyList<ExportEmployeeDto>>> EmployeesAsync(Guid actorId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(actorId, ct);
        if (access.IsFailure) return Result<IReadOnlyList<ExportEmployeeDto>>.Failure(access.Errors.ToArray());
        if (!ValidPage(page, pageSize) || search?.Length > 200)
            return Result<IReadOnlyList<ExportEmployeeDto>>.Failure(new Error("InvalidFilter", "Use a short search and valid pagination."));
        return Result<IReadOnlyList<ExportEmployeeDto>>.Success(await exports.EmployeesAsync(search?.Trim(), page, pageSize, ct));
    }

    public async Task<Result> CloseTimeEntriesAsync(Guid actorId, Guid id, CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(actorId, ct);
        if (access.IsFailure) return Result.Failure(access.Errors.ToArray());
        var typed = ExportId.Create(id);
        if (typed.IsFailure) return Result.Failure(typed.Errors.ToArray());
        return await exports.CloseTimeEntriesAsync(typed.Value, access.Value, clock.GetUtcNow().UtcDateTime, ct);
    }

    private async Task<Result<EmployeeId>> AuthorizeAsync(Guid actorId, CancellationToken ct)
    {
        var id = EmployeeId.Create(actorId);
        if (id.IsFailure) return Result<EmployeeId>.Failure(id.Errors.ToArray());
        var actor = await employees.GetWithAccessesAsync(id.Value, ct);
        return actor is not null && actor.HasPermission(SystemPermission.Employee) &&
            (actor.HasPermission(SystemPermission.Payroll) || actor.HasPermission(SystemPermission.Administrator))
            ? Result<EmployeeId>.Success(id.Value)
            : Result<EmployeeId>.Failure(new Error("ExportForbidden", "Active payroll or administrator access is required."));
    }

    private static bool ValidPage(int page, int size) => page >= 1 && size is >= 1 and <= 100 && (long)(page - 1) * size <= int.MaxValue;
    public static ExportSummaryDto Summary(Export batch) => new(batch.Id.Value, batch.ExportedBy.Value, batch.CreatedAt,
        batch.DateFrom, batch.DateTo, batch.SelectedEmployeeId?.Value, batch.TimeEntries.Count, batch.TotalHours,
        batch.TimeEntriesClosedBy?.Value, batch.TimeEntriesClosedAtUtc);
    public static ExportDetailsDto Details(Export batch) => new(Summary(batch), batch.TimeEntries
        .OrderBy(item => item.TimeEntryId.Value).Select(item => new ExportEntryDto(item.TimeEntryId.Value, item.TimeEntryAuditId.Value)).ToArray(),
        $"/api/exports/{batch.Id.Value}/download");
}
