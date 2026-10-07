using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

public sealed class UpdateTimeEntry(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [HttpPut("time_entries/{id:guid}")]
    public async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateTimeEntryRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var employeeId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(new UpdateTimeEntryCommand(
            id,
            employeeId.Value,
            request.TaskId,
            request.Date,
            request.Hours,
            request.Comment));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
