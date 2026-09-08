using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
namespace Backend.TimeEntries;


public class CreateTimeEntry(
    ICommandDispatcher Dispatcher,
    IMapper Mapper)
    : ApiEndpoint
        .WithRequest<CreateTimeEntry.CreateTimeEntryRequest>
        .AndResponse<
            Results<
                Ok<CreateTimeEntry.CreateTimeEntryResponse>,
                BadRequest<IEnumerable<Error>>>>
{
    [HttpPost("time_entries")]
    public override async Task<
            Results<
                Ok<CreateTimeEntryResponse>,
                BadRequest<IEnumerable<Error>>>>
        HandleAsync(CreateTimeEntryRequest request)
    {
        var command =
            Mapper.Map<CreateTimeEntryCommand>(request);

        var dispatchResult =
            await Dispatcher.DispatchAsync(command);

        if (dispatchResult.IsSuccess)
        {
            var response =
                Mapper.Map<CreateTimeEntryResponse>(command);

            return TypedResults.Ok(response);
        }

        return TypedResults.BadRequest(
            (IEnumerable<Error>)dispatchResult.Errors);
    }
public record CreateTimeEntryRequest(
    [FromBody] CreateTimeEntryRequest.CreateTimeEntryBody RequestBody)
{
    public record CreateTimeEntryBody(
        int WorkItemId,
        DateTime Date,
        decimal Hours,
        TimeSpan? StartTime,
        TimeSpan? EndTime,
        string? Comment);
}

public record CreateTimeEntryResponse(
    int Id,
    int WorkItemId,
    DateTime Date,
    decimal Hours,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    string? Comment);
}
