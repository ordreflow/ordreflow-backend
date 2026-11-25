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

/// <summary>Lists the employees an export can be created for, one page at a time.</summary>
public sealed class ListExportEmployees(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<ListExportEmployeesRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Export)]
    [HttpGet("exports/employees")]
    [ProducesResponseType(typeof(ExportPage<ExportEmployeeDto>), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync([FromQuery] ListExportEmployeesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<ExportEmployeesQuery, Result<IReadOnlyList<ExportEmployeeDto>>>(
            new(actor, request.Search, request.Page, request.PageSize));
        return result.IsFailure ? result.Errors.ToErrorResult()
            : TypedResults.Ok(new ExportPage<ExportEmployeeDto>(result.Value, request.Page, request.PageSize));
    }
}
