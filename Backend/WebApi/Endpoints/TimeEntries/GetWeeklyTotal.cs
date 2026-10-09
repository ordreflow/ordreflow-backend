using Application;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

public sealed class GetWeeklyTotal(
    IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<ViewTimeEntriesRequest>.AndResponse<IResult>
{
    [HttpGet("time_entries/weekly-total")]
    public override async Task<IResult> HandleAsync( [FromQuery] ViewTimeEntriesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var employeeId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetMyWeeklyTotalQuery, Result<decimal>>(
            new GetMyWeeklyTotalQuery(
                employeeId,
                request.FromDate.ToDateTime(TimeOnly.MinValue),
                request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue)));

        if (result.IsFailure)
            return TypedResults.BadRequest(result.Errors);

        return TypedResults.Ok(new WeeklyTotalResponse(
            request.FromDate,
            request.ToDate,
            result.Value));
    }
}
