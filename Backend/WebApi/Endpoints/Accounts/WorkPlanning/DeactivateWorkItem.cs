using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Accounts.WorkPlanning;

/// <summary>Deactivated work can no longer be selected for time registration.</summary>
public sealed class DeactivateWorkItem(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.WorkPlanning)]
    [HttpPost("work-items/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<SetWorkItemActiveCommand, bool>(new SetWorkItemActiveCommand(actor, id, false), HttpContext.RequestAborted);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
