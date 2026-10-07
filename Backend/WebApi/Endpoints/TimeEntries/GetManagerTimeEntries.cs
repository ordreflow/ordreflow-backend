using Application;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

public sealed class GetManagerTimeEntries(
    IQueryHandler<GetManagerTimeEntriesQuery, Result<IReadOnlyList<TimeEntry>>> queryHandler,
    IMapper mapper)
    : ApiEndpoint.WithRequest<ViewTimeEntriesRequest>.AndResponse<IResult>
{
    [HttpGet("manager/time_entries")]
    public override async Task<IResult> HandleAsync(ViewTimeEntriesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var managerId))
            return TypedResults.Unauthorized();

        var result = await queryHandler.HandleAsync(new GetManagerTimeEntriesQuery(
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
