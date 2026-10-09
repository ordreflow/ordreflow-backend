using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.TimeEntries;

/// <summary>
/// Resubmits a returned time entry after correction, moving it back to Draft.
/// </summary>
public sealed class ResubmitTimeEntry(ICommandDispatcher dispatcher)
    : EndpointBase
{
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
