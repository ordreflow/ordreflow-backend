using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
namespace Backend.TimeEntries;

public class getTimeEntries(ICommandDispatcher dispatcher, IMapper mapper)
    : ApiEndpoint
        .WithRequest<GetTimeEntriesRequest>
        .AndResponse<
            Results<
                Ok<GetTimeEntriesResponse>,
                BadRequest<IEnumerable<Error>>>>
{
    [HttpGet("time_entries")]
    public override async Task<
    Results<
        Ok<GetTimeEntriesResponse>,
        BadRequest<IEnumerable<Error>>>>
    HandleAsync(
        [FromQuery] GetTimeEntriesRequest request)
    {
        var command = new GetTimeEntriesCommand(
            request.StartDate,
            request.EndDate);

        var dispatchResult =
            await dispatcher.DispatchAsync(command);

        if (dispatchResult.IsSuccess)
        {
            var timeEntries =
                ((Result<IEnumerable<TimeEntry>>)dispatchResult).Value;

            var response =
                mapper.Map<GetTimeEntriesResponse>(timeEntries);

            return TypedResults.Ok(response);
        }

        return TypedResults.BadRequest(
            (IEnumerable<Error>)dispatchResult.Errors);
    }
 
}
public record GetTimeEntriesRequest(
    DateTime StartDate,
    DateTime EndDate);


public record GetTimeEntriesResponse(
    IEnumerable<TimeEntryDto> TimeEntries);

public record TimeEntryDto(
    int Id,
    int WorkItemId,
    DateTime Date,
    decimal Hours,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    string? Comment);
