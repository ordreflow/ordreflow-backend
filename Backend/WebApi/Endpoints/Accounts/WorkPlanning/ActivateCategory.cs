using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Accounts.WorkPlanning;

/// <summary>Activates a category so its work can be selected for time registration again.</summary>
public sealed class ActivateCategory(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.WorkPlanning)]
    [HttpPost("categories/{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<SetCategoryActiveCommand, bool>(new SetCategoryActiveCommand(actor, id, true), HttpContext.RequestAborted);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
