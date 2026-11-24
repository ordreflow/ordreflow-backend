using Application;
using Application.Commands;
using Application.Exports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Exports;

namespace WebAPI.Endpoints.Exports;

/// <summary>Creates an export of accepted time entries for a period.</summary>
public sealed class CreateExport(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateExportRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Export)]
    [HttpPost("exports")]
    [ProducesResponseType(typeof(ExportDetailsResponse), StatusCodes.Status201Created)]
    public override async Task<IResult> HandleAsync([FromBody] CreateExportRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<CreateExportCommand, ExportDetailsDto>(
            new CreateExportCommand(actor, request.ExportId, request.FromDate, request.ToDate, request.EmployeeId),
            HttpContext.RequestAborted);
        return result.IsFailure ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/exports/{result.Value.Export.Id}", ExportResponses.ToResponse(result.Value));
    }
}
