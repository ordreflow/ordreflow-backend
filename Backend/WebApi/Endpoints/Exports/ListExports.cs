using Application;
using Application.Exports;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Exports;

namespace WebAPI.Endpoints.Exports;

/// <summary>Lists previous exports, one page at a time.</summary>
public sealed class ListExports(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<ListExportsRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Export)]
    [HttpGet("exports")]
    [ProducesResponseType(typeof(ExportPage<ExportSummaryDto>), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync([FromQuery] ListExportsRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<ListExportsQuery, Result<IReadOnlyList<ExportSummaryDto>>>(
            new(actor, request.Page, request.PageSize));
        return result.IsFailure ? result.Errors.ToErrorResult()
            : TypedResults.Ok(new ExportPage<ExportSummaryDto>(result.Value, request.Page, request.PageSize));
    }
}
