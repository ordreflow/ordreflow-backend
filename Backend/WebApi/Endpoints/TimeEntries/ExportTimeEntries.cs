using Application;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.TimeEntries;

public sealed class ExportTimeEntries(
    IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<ExportTimeEntriesRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.UnavailableUntilRefactored)]
    [HttpGet("time_entries/export")]
    public override async Task<IResult> HandleAsync( [FromQuery] ExportTimeEntriesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var requesterId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<ExportAcceptedTimeEntriesQuery, Result<IReadOnlyList<TimeEntryExportRow>>>(
            new ExportAcceptedTimeEntriesQuery(
                requesterId,
                request.FromDate.ToDateTime(TimeOnly.MinValue),
                request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                request.EmployeeId));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return Results.File(
            TimeEntryExportCsv.Create(result.Value),
            "text/csv; charset=utf-8",
            $"time-entries-{request.FromDate:yyyyMMdd}-{request.ToDate:yyyyMMdd}.csv");
    }
}
