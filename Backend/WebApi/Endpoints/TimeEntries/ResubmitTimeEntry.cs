using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.TimeEntries;

/// <summary>
/// Moves a returned time entry back to Draft without an edit. Editing also moves it to Draft automatically.
/// </summary>
public sealed class ResubmitTimeEntry(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [Authorize(Policy = AuthorizationPolicies.AnyRole)]
    [HttpPost("time_entries/{id:guid}/resubmit")]
    public async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var employeeId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(
            new ResubmitTimeEntryCommand(id, employeeId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
