using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetMyTimeEntriesForWeekHandler(
    ITimeEntryRepository repository) : IQueryHandler<
        GetMyTimeEntriesForWeekQuery,
        Result<IReadOnlyList<TimeEntryDto>>>
{
    public async Task<Result<IReadOnlyList<TimeEntryDto>>> HandleAsync(
        GetMyTimeEntriesForWeekQuery query)
    {
        if (query.WeekEndExclusive <= query.WeekStart)
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(
                new Error("InvalidDateRange", "The week end must be after the week start."));

        var employeeIdResult = UserId.Create(query.EmployeeId);

        if (employeeIdResult.IsFailure)
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(employeeIdResult.Errors.ToArray());

        var entries = await repository.GetByEmployeeIdAsync(employeeIdResult.Value);
        var filtered = entries
            .Where(entry => entry.Date >= query.WeekStart && entry.Date < query.WeekEndExclusive)
            .OrderBy(entry => entry.Date)
            .Select(ToDto)
            .ToArray();

        return Result<IReadOnlyList<TimeEntryDto>>.Success(filtered);
    }

    private static TimeEntryDto ToDto(TimeEntry entry) => new(
        entry.Id.Value,
        entry.TaskId.Value,
        entry.Date,
        entry.Hours,
        entry.Comment);
}
