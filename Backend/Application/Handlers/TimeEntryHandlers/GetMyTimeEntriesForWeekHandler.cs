using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;

namespace Application.Handlers;

public sealed class GetMyTimeEntriesForWeekHandler(
    ITimeEntryRepository repository) : IQueryHandler<
        GetMyTimeEntriesForWeekQuery,
        Result<IReadOnlyList<TimeEntry>>>
{
    public async Task<Result<IReadOnlyList<TimeEntry>>> HandleAsync(
        GetMyTimeEntriesForWeekQuery query)
    {
        if (query.WeekEndExclusive <= query.WeekStart)
            return Result<IReadOnlyList<TimeEntry>>.Failure(
                new Error("InvalidDateRange", "The week end must be after the week start."));

        var entries = await repository.GetByEmployeeIdAsync(query.EmployeeId);
        var filtered = entries
            .Where(entry => entry.Date >= query.WeekStart && entry.Date < query.WeekEndExclusive)
            .OrderBy(entry => entry.Date)
            .ToArray();

        return Result<IReadOnlyList<TimeEntry>>.Success(filtered);
    }
}
