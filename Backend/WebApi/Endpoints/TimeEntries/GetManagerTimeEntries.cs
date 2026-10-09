using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

public sealed class GetManagerTimeEntries(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint.WithRequest<ViewTimeEntriesRequest>.AndResponse<IResult>
{
    [HttpGet("time_entries/manager")]
    public override async Task<IResult> HandleAsync(ViewTimeEntriesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var managerId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetManagerTimeEntriesQuery, Result<IReadOnlyList<TimeEntryDto>>>(
            new GetManagerTimeEntriesQuery(
                managerId,
                request.FromDate.ToDateTime(TimeOnly.MinValue),
                request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                null));

        if (result.IsFailure)
            return TypedResults.BadRequest(result.Errors);

        return TypedResults.Ok(new ViewTimeEntriesResponse(result.Value
            .Select(mapper.Map<TimeEntryResponse>)
            .ToArray()));
    }
}
