using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.TimeEntryReviews;

/// <summary>
/// Approves a ready time entry in an assigned category.
/// </summary>
public sealed class ApproveTimeEntry(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [Authorize(Policy = AuthorizationPolicies.CatagoryReviewer)]
    [HttpPost("time_entries/{id:guid}/accept")]
    [HttpPost("time_entries/{id:guid}/approve")]
    public async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var reviewerId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(
            new ApproveTimeEntryCommand(id, reviewerId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
