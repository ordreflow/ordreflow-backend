using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

public sealed class GetTimeEntries(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint.WithRequest<ViewTimeEntriesRequest>.AndResponse<IResult>
{
    [HttpGet("time_entries")]
    public override async Task<IResult> HandleAsync(ViewTimeEntriesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var employeeId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetMyTimeEntriesForWeekQuery, Result<IReadOnlyList<TimeEntryDto>>>(
            new GetMyTimeEntriesForWeekQuery(
                employeeId,
                request.FromDate.ToDateTime(TimeOnly.MinValue),
                request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue)));

        if (result.IsFailure)
            return TypedResults.BadRequest(result.Errors);

        var response = new ViewTimeEntriesResponse(result.Value
            .Select(mapper.Map<TimeEntryResponse>)
            .ToArray());

        return TypedResults.Ok(response);
    }
}
