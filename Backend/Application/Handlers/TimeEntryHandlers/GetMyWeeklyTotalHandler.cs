using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetMyWeeklyTotalHandler(
    ITimeEntryRepository repository) : IQueryHandler<
        GetMyWeeklyTotalQuery,
        Result<decimal>>
{
    public async Task<Result<decimal>> HandleAsync(GetMyWeeklyTotalQuery query)
    {
        var employeeId = EmployeeId.Create(query.EmployeeId);
        if (employeeId.IsFailure)
            return Result<decimal>.Failure(employeeId.Errors.ToArray());

        var week = GetMyTimeEntriesForWeekHandler.CreateWeek(
            query.WeekStart,
            query.WeekEndExclusive);
        if (week.IsFailure)
            return Result<decimal>.Failure(week.Errors.ToArray());

        var entries = await repository.GetByEmployeeIdAsync(employeeId.Value);
        var total = entries
            .Select(entry => entry.GetCurrentAudit())
            .Where(audit => audit is { Status: not TimeEntryStatus.Deleted } &&
                            audit.WorkDate.Value >= week.Value.StartsOn &&
                            audit.WorkDate.Value < week.Value.EndsOnExclusive)
            .Sum(audit => (decimal)audit!.Duration.Value);

        return Result<decimal>.Success(total);
    }
}
