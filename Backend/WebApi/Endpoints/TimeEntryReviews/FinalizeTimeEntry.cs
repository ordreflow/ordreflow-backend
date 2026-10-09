using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.TimeEntryReviews;

/// <summary>
/// Finalizes an accepted time entry.
/// </summary>
public sealed class FinalizeTimeEntry(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [HttpPost("time_entries/{id:guid}/finalize")]
    public async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var reviewerId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(
            new FinalizeTimeEntryCommand(id, reviewerId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
