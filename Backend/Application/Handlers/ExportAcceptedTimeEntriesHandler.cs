using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ExportAcceptedTimeEntriesHandler(
    ITimeEntryRepository timeEntryRepository,
    IUserRepository userRepository) : IQueryHandler<
        ExportAcceptedTimeEntriesQuery,
        Result<IReadOnlyList<TimeEntryExportRow>>>
{
    public async Task<Result<IReadOnlyList<TimeEntryExportRow>>> HandleAsync(
        ExportAcceptedTimeEntriesQuery query)
    {
        if (query.ToDateExclusive <= query.FromDate)
            return Result<IReadOnlyList<TimeEntryExportRow>>.Failure(
                new Error("InvalidDateRange", "The export end must be after the export start."));

        var requester = await userRepository.GetAsync(query.RequestedBy);
        if (requester is null || requester.Status != UserStatus.Active ||
            requester.Role is not (UserRole.Manager or UserRole.Admin))
            return Result<IReadOnlyList<TimeEntryExportRow>>.Failure(
                new Error("ExportForbidden", "The user is not authorized to export time entries."));

        var entries = await timeEntryRepository.GetByDateRangeAsync(
            query.FromDate,
            query.ToDateExclusive);

        var rows = entries
            .Where(entry => entry.Status is TimeEntryStatus.Accepted or TimeEntryStatus.Finalized)
            .Select(entry => new TimeEntryExportRow(
                entry.Id,
                entry.EmployeeId,
                entry.TaskId,
                entry.Date,
                entry.Hours,
                entry.Comment,
                entry.Status))
            .OrderBy(row => row.Date)
            .ToArray();

        return Result<IReadOnlyList<TimeEntryExportRow>>.Success(rows);
    }
}
