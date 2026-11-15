using Application;
using Application.Commands;
using Application.Exports;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Exports;

namespace WebAPI.Endpoints.Exports;

[Authorize(Policy = AuthorizationPolicies.Export)]
public sealed class ExportsController(ICommandDispatcher commands, IQueryDispatcher queries) : EndpointBase
{
    [HttpPost("exports")]
    [ProducesResponseType(typeof(ExportDetailsResponse), StatusCodes.Status201Created)]
    public async Task<IResult> Create([FromBody] CreateExportRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<CreateExportCommand, ExportDetailsDto>(
            new CreateExportCommand(actor, request.ExportId, request.FromDate, request.ToDate, request.EmployeeId), ct);
        return result.IsFailure ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/exports/{result.Value.Export.Id}", ToResponse(result.Value));
    }

    [HttpGet("exports")]
    [ProducesResponseType(typeof(ExportPage<ExportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<ListExportsQuery, Result<IReadOnlyList<ExportSummaryDto>>>(new(actor, page, pageSize));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ExportPage<ExportSummaryDto>(result.Value, page, pageSize));
    }

    [HttpGet("exports/employees")]
    [ProducesResponseType(typeof(ExportPage<ExportEmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IResult> Employees([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<ExportEmployeesQuery, Result<IReadOnlyList<ExportEmployeeDto>>>(new(actor, search, page, pageSize));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ExportPage<ExportEmployeeDto>(result.Value, page, pageSize));
    }

    [HttpGet("exports/{id:guid}")]
    [ProducesResponseType(typeof(ExportDetailsResponse), StatusCodes.Status200OK)]
    public async Task<IResult> Details([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<GetExportQuery, Result<ExportDetailsDto>>(new(actor, id));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(ToResponse(result.Value));
    }

    [HttpGet("exports/{id:guid}/download")]
    [Produces("text/csv")]
    public async Task<IResult> Download([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<DownloadExportQuery, Result<ExportDownloadDto>>(new(actor, id));
        if (result.IsFailure) return result.Errors.ToErrorResult();
        var file = result.Value;
        return TypedResults.File(file.Content, "text/csv; charset=utf-8", $"time-entries-{file.FromDate:yyyyMMdd}-{file.ToDate:yyyyMMdd}-{file.Id}.csv");
    }

    [HttpPost("exports/{id:guid}/close-time-entries")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> CloseTimeEntries([FromRoute] Guid id, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<CloseExportTimeEntriesCommand, bool>(new(actor, id), ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }

    private static ExportDetailsResponse ToResponse(ExportDetailsDto details) =>
        new(details.Export, details.Entries, $"/api/exports/{details.Export.Id}/download");
}
