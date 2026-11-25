using Application.Commands;
using Application.Exports;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateExportHandler(ExportAccess access, IExportRepository exports, TimeProvider clock)
    : ICommandHandler<CreateExportCommand, ExportDetailsDto>
{
    public async Task<Result<ExportDetailsDto>> HandleAsync(CreateExportCommand request, CancellationToken ct = default)
    {
        var authorized = await access.RequireAsync(request.RequestedBy, ct);
        if (authorized.IsFailure) return Result<ExportDetailsDto>.Failure(authorized.Errors.ToArray());
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
            return existing.MatchesRequest(authorized.Value, request.FromDate, request.ToDate, employee)
                ? Result<ExportDetailsDto>.Success(ExportProjection.Details(existing))
                : Result<ExportDetailsDto>.Failure(new Error("ExportRequestConflict", "This export ID was used for a different request."));
        if (employee is not null && !await exports.EmployeeExistsAsync(employee, ct))
            return Result<ExportDetailsDto>.Failure(new Error("EmployeeNotFound", "The selected employee was not found."));
        var selected = await exports.SelectApprovedAsync(request.FromDate, request.ToDate, employee, ct);
        var batch = Export.Create(id.Value, authorized.Value, request.FromDate, request.ToDate, employee,
            selected.Select(item => item.Entry).ToArray(), ExportCsv.Create(selected), clock.GetUtcNow().UtcDateTime);
        if (batch.IsFailure) return Result<ExportDetailsDto>.Failure(batch.Errors.ToArray());
        await exports.AddAsync(batch.Value, ct);
        return Result<ExportDetailsDto>.Success(ExportProjection.Details(batch.Value));
    }
}
