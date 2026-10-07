using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;

namespace Application.Handlers;

public sealed class GetMyWeeklyTotalHandler(
    ITimeEntryRepository repository) : IQueryHandler<
        GetMyWeeklyTotalQuery,
        Result<decimal>>
{
    public async Task<Result<decimal>> HandleAsync(GetMyWeeklyTotalQuery query)
    {
        if (query.WeekEndExclusive <= query.WeekStart)
            return Result<decimal>.Failure(
                new Error("InvalidDateRange", "The week end must be after the week start."));

        var entries = await repository.GetByEmployeeIdAsync(query.EmployeeId);
        var total = entries
            .Where(entry => entry.Date >= query.WeekStart && entry.Date < query.WeekEndExclusive)
            .Sum(entry => entry.Hours);

        return Result<decimal>.Success(total);
    }
}
