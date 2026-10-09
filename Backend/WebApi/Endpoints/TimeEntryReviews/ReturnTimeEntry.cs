using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntryReviews;

/// <summary>
/// Returns a draft time entry to the employee for correction, with a reason.
/// </summary>
public sealed class ReturnTimeEntry(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [HttpPost("time_entries/{id:guid}/return")]
    public async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ReturnTimeEntryRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var reviewerId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(
            new ReturnTimeEntryCommand(id, reviewerId, request.Reason));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
