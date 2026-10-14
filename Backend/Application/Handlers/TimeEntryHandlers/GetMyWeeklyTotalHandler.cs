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
        if (query.WeekEndExclusive <= query.WeekStart)
            return Result<decimal>.Failure(
                new Error("InvalidDateRange", "The week end must be after the week start."));

        var employeeIdResult = UserId.Create(query.EmployeeId);

        if (employeeIdResult.IsFailure)
            return Result<decimal>.Failure(employeeIdResult.Errors.ToArray());

        var entries = await repository.GetByEmployeeIdAsync(employeeIdResult.Value);
        var total = entries
            .Where(entry => entry.Date >= query.WeekStart && entry.Date < query.WeekEndExclusive)
            .Sum(entry => entry.Hours);

        return Result<decimal>.Success(total);
    }
}
