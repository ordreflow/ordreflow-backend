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

/// <summary>Gets the details of one export.</summary>
public sealed class GetExport(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Export)]
    [HttpGet("exports/{id:guid}")]
    [ProducesResponseType(typeof(ExportDetailsResponse), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<GetExportQuery, Result<ExportDetailsDto>>(new(actor, id));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(ExportResponses.ToResponse(result.Value));
    }
}
