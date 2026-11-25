using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.TimeEntryReviews;

/// <summary>
/// Rejects a ready time entry in an assigned category, with a reason.
/// </summary>
public sealed class RejectTimeEntry(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [Authorize(Policy = AuthorizationPolicies.CatagoryReviewer)]
    [HttpPost("time_entries/{id:guid}/return")]
    [HttpPost("time_entries/{id:guid}/reject")]
    public async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] RejectTimeEntryRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var reviewerId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(
            new RejectTimeEntryCommand(id, reviewerId, request.Reason));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
