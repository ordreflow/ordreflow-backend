using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Exports;

/// <summary>Closes the time entries included in an export.</summary>
public sealed class CloseExportTimeEntries(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Export)]
    [HttpPost("exports/{id:guid}/close-time-entries")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<CloseExportTimeEntriesCommand, bool>(
            new(actor, id), HttpContext.RequestAborted);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
