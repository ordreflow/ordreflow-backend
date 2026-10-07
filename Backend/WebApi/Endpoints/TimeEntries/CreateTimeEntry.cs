using Application;
using Application.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

public sealed class CreateTimeEntry(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateTimeEntryRequest>.AndResponse<IResult>
{
    [HttpPost("time_entries")]
    public override async Task<IResult> HandleAsync(CreateTimeEntryRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var employeeId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(new CreateTimeEntryCommand(
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
