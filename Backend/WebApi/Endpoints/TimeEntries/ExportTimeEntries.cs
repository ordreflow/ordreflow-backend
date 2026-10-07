using Application;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

public sealed class ExportTimeEntries(
    IQueryHandler<ExportAcceptedTimeEntriesQuery, Result<IReadOnlyList<TimeEntryExportRow>>> queryHandler)
    : ApiEndpoint.WithRequest<ExportTimeEntriesRequest>.AndResponse<IResult>
{
    [HttpGet("time_entries/export")]
    public override async Task<IResult> HandleAsync(ExportTimeEntriesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var requesterId))
            return TypedResults.Unauthorized();

        var result = await queryHandler.HandleAsync(new ExportAcceptedTimeEntriesQuery(
            requesterId,
            request.FromDate.ToDateTime(TimeOnly.MinValue),
            request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue)));

        if (result.IsFailure)
            return TypedResults.BadRequest(result.Errors);

        var csv = new StringBuilder();
        csv.AppendLine("TimeEntryId,EmployeeId,TaskId,Date,Hours,Comment,Status");
        foreach (var row in result.Value)
        {
            var comment = row.Comment?.Replace("\"", "\"\"") ?? string.Empty;
            csv.AppendLine($"{row.TimeEntryId.Value},{row.EmployeeId.Value},{row.TaskId.Value},{row.Date:O},{row.Hours},\"{comment}\",{row.Status}");
        }

        return Results.File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            $"time-entries-{request.FromDate:yyyyMMdd}-{request.ToDate:yyyyMMdd}.csv");
    }
}
