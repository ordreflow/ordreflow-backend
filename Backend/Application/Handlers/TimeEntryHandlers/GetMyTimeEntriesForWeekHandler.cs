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
        var employeeId = EmployeeId.Create(query.EmployeeId);
        if (employeeId.IsFailure)
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(employeeId.Errors.ToArray());

        var week = CreateWeek(query.WeekStart, query.WeekEndExclusive);
        if (week.IsFailure)
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(week.Errors.ToArray());

        var entries = await repository.GetByEmployeeIdAsync(employeeId.Value);
        IReadOnlyList<TimeEntryDto> result = entries
            .Select(entry => (Entry: entry, Audit: entry.GetCurrentAudit()))
            .Where(item => item.Audit is { Status: not TimeEntryStatus.Deleted } &&
                           item.Audit.WorkDate.Value >= week.Value.StartsOn &&
                           item.Audit.WorkDate.Value < week.Value.EndsOnExclusive)
            .OrderBy(item => item.Audit!.WorkDate.Value)
            .ThenBy(item => item.Entry.Id.Value)
            .Select(item => ToDto(item.Entry, item.Audit!))
            .ToArray();

        return Result<IReadOnlyList<TimeEntryDto>>.Success(result);
    }

    internal static Result<TimeEntryWeekRange> CreateWeek(
        DateTime weekStart,
        DateTime weekEndExclusive)
    {
        if (weekStart == default || weekEndExclusive == default ||
            weekStart.TimeOfDay != TimeSpan.Zero ||
            weekEndExclusive.TimeOfDay != TimeSpan.Zero)
            return Result<TimeEntryWeekRange>.Failure(
                new Error("InvalidDateRange", "A week must use date-only boundaries."));

        var startsOn = DateOnly.FromDateTime(weekStart);
        if (startsOn.DayOfWeek != DayOfWeek.Monday ||
            startsOn > DateOnly.MaxValue.AddDays(-7) ||
            weekEndExclusive != startsOn.AddDays(7).ToDateTime(TimeOnly.MinValue))
            return Result<TimeEntryWeekRange>.Failure(
                new Error("InvalidDateRange", "The requested range must be one full Monday-to-Sunday week."));

        return Result<TimeEntryWeekRange>.Success(
            new TimeEntryWeekRange(startsOn, startsOn.AddDays(7)));
    }

    private static TimeEntryDto ToDto(TimeEntry entry, Domain.Entities.TimeEntryAudit audit) => new(
        entry.Id.Value,
        audit.TaskId.Value,
        audit.WorkDate.Value.ToDateTime(TimeOnly.MinValue),
        (decimal)audit.Duration.Value,
        audit.Description.Value,
        entry.EmployeeId.Value,
        audit.Status.ToString(),
        entry.CurrentRejectionReason?.Value);
}

internal sealed record TimeEntryWeekRange(DateOnly StartsOn, DateOnly EndsOnExclusive);
