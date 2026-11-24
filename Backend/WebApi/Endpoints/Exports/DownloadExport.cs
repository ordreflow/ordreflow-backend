using Application;
using Application.Exports;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Exports;

/// <summary>Downloads an export as a CSV file.</summary>
public sealed class DownloadExport(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Export)]
    [HttpGet("exports/{id:guid}/download")]
    [Produces("text/csv")]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<DownloadExportQuery, Result<ExportDownloadDto>>(new(actor, id));
        if (result.IsFailure) return result.Errors.ToErrorResult();
        var file = result.Value;
        return TypedResults.File(file.Content, "text/csv; charset=utf-8", $"time-entries-{file.FromDate:yyyyMMdd}-{file.ToDate:yyyyMMdd}-{file.Id}.csv");
    }
}
