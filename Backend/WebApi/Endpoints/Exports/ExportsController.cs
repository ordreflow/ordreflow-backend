using Application.Exports;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Exports;

namespace WebAPI.Endpoints.Exports;

[Authorize(Policy = AuthorizationPolicies.Export)]
public sealed class ExportsController(PayrollExportService exports) : EndpointBase
{
    [HttpPost("exports")]
    [ProducesResponseType(typeof(ExportDetailsDto), StatusCodes.Status201Created)]
    public async Task<IResult> Create([FromBody] CreateExportRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await exports.CreateAsync(actor,
            new CreateExportCommand(request.ExportId, request.FromDate, request.ToDate, request.EmployeeId), ct);
        return result.IsFailure ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/exports/{result.Value.Export.Id}", result.Value);
    }

    [HttpGet("exports")]
    [ProducesResponseType(typeof(ExportPage<ExportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await exports.ListAsync(actor, page, pageSize, ct);
        return result.IsFailure ? result.Errors.ToErrorResult()
            : TypedResults.Ok(new ExportPage<ExportSummaryDto>(result.Value, page, pageSize));
    }

    [HttpGet("exports/employees")]
    [ProducesResponseType(typeof(ExportPage<ExportEmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IResult> Employees(CancellationToken ct, [FromQuery] string? search = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await exports.EmployeesAsync(actor, search, page, pageSize, ct);
        return result.IsFailure ? result.Errors.ToErrorResult()
            : TypedResults.Ok(new ExportPage<ExportEmployeeDto>(result.Value, page, pageSize));
    }

    [HttpGet("exports/{id:guid}")]
    [ProducesResponseType(typeof(ExportDetailsDto), StatusCodes.Status200OK)]
    public async Task<IResult> Details([FromRoute] Guid id, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await exports.GetAsync(actor, id, ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(PayrollExportService.Details(result.Value));
    }

    [HttpGet("exports/{id:guid}/download")]
    [Produces("text/csv")]
    public async Task<IResult> Download([FromRoute] Guid id, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await exports.GetAsync(actor, id, ct);
        if (result.IsFailure) return result.Errors.ToErrorResult();
        var batch = result.Value;
        return TypedResults.File(batch.CsvContent, "text/csv; charset=utf-8",
            $"time-entries-{batch.DateFrom:yyyyMMdd}-{batch.DateTo:yyyyMMdd}-{batch.Id.Value}.csv");
    }

    [HttpPost("exports/{id:guid}/close-time-entries")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> CloseTimeEntries([FromRoute] Guid id, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await exports.CloseTimeEntriesAsync(actor, id, ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
