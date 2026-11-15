using Application.Exports;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ExportReadHandlers(ExportAccess access, IExportRepository exports) :
    IQueryHandler<GetExportQuery, Result<ExportDetailsDto>>,
    IQueryHandler<DownloadExportQuery, Result<ExportDownloadDto>>,
    IQueryHandler<ListExportsQuery, Result<IReadOnlyList<ExportSummaryDto>>>,
    IQueryHandler<ExportEmployeesQuery, Result<IReadOnlyList<ExportEmployeeDto>>>
{
    public async Task<Result<ExportDetailsDto>> HandleAsync(GetExportQuery query)
    {
        var result = await LoadAsync(query.ActorId, query.ExportId);
        return result.IsFailure ? Result<ExportDetailsDto>.Failure(result.Errors.ToArray())
            : Result<ExportDetailsDto>.Success(ExportProjection.Details(result.Value));
    }

    public async Task<Result<ExportDownloadDto>> HandleAsync(DownloadExportQuery query)
    {
        var result = await LoadAsync(query.ActorId, query.ExportId);
        return result.IsFailure ? Result<ExportDownloadDto>.Failure(result.Errors.ToArray())
            : Result<ExportDownloadDto>.Success(new ExportDownloadDto(result.Value.Id.Value,
                result.Value.DateFrom, result.Value.DateTo, result.Value.CsvContent));
    }

    public async Task<Result<IReadOnlyList<ExportSummaryDto>>> HandleAsync(ListExportsQuery query)
    {
        var actor = await access.RequireAsync(query.ActorId, default);
        if (actor.IsFailure) return Result<IReadOnlyList<ExportSummaryDto>>.Failure(actor.Errors.ToArray());
        if (!ValidPage(query.Page, query.PageSize))
            return Result<IReadOnlyList<ExportSummaryDto>>.Failure(new Error("InvalidPage", "Use page >= 1 and pageSize 1–100."));
        return Result<IReadOnlyList<ExportSummaryDto>>.Success(await exports.ListAsync(query.Page, query.PageSize, default));
    }

    public async Task<Result<IReadOnlyList<ExportEmployeeDto>>> HandleAsync(ExportEmployeesQuery query)
    {
        var actor = await access.RequireAsync(query.ActorId, default);
        if (actor.IsFailure) return Result<IReadOnlyList<ExportEmployeeDto>>.Failure(actor.Errors.ToArray());
        if (!ValidPage(query.Page, query.PageSize) || query.Search?.Length > 200)
            return Result<IReadOnlyList<ExportEmployeeDto>>.Failure(new Error("InvalidFilter", "Use a short search and valid pagination."));
        return Result<IReadOnlyList<ExportEmployeeDto>>.Success(await exports.EmployeesAsync(query.Search?.Trim(), query.Page, query.PageSize, default));
    }

    private async Task<Result<Export>> LoadAsync(Guid actorId, Guid exportId)
    {
        var actor = await access.RequireAsync(actorId, default);
        if (actor.IsFailure) return Result<Export>.Failure(actor.Errors.ToArray());
        var id = ExportId.Create(exportId);
        if (id.IsFailure) return Result<Export>.Failure(id.Errors.ToArray());
        var batch = await exports.GetAsync(id.Value, default);
        return batch is null ? Result<Export>.Failure(new Error("ExportNotFound", "The export was not found.")) : Result<Export>.Success(batch);
    }

    private static bool ValidPage(int page, int size) => page >= 1 && size is >= 1 and <= 100 && (long)(page - 1) * size <= int.MaxValue;
}
