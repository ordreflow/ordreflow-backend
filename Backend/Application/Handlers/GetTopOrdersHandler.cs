using Application.Dtos;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;

namespace Application.Handlers;

public sealed class GetTopOrdersHandler(ITopOrdersRepository repository)
    : IQueryHandler<
        GetTopOrdersQuery,
        Result<IReadOnlyList<TopOrdersReportDto>>>
{
    public async Task<Result<IReadOnlyList<TopOrdersReportDto>>> HandleAsync(
        GetTopOrdersQuery query)
    {
        if (query.ToDate < query.FromDate)
        {
            return Result<IReadOnlyList<TopOrdersReportDto>>.Failure(
                new Error("InvalidDateRange", "The end date must be on or after the start date."));
        }

        if (query.Limit is < 1 or > 10)
        {
            return Result<IReadOnlyList<TopOrdersReportDto>>.Failure(
                new Error("InvalidLimit", "The top orders report limit must be between 1 and 10."));
        }

        var cases = await repository.GetTopOrdersAsync(
            query.FromDate,
            query.ToDate,
            query.Limit,
            query.OnlyAccepted);

        return Result<IReadOnlyList<TopOrdersReportDto>>.Success(cases);
    }
}
