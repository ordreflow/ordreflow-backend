using Application.Dtos;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetTopOrdersHandler(
    ITopOrdersRepository repository,
    IUserRepository userRepository)
    : IQueryHandler<
        GetTopOrdersQuery,
        Result<IReadOnlyList<TopOrdersReportDto>>>
{
    public async Task<Result<IReadOnlyList<TopOrdersReportDto>>> HandleAsync(
        GetTopOrdersQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);
        if (actorIdResult.IsFailure)
        {
            return Result<IReadOnlyList<TopOrdersReportDto>>.Failure(
                actorIdResult.Errors.ToArray());
        }

        var actor = await userRepository.GetAsync(actorIdResult.Value);
        if (actor is null || !actor.CanReviewTimeEntries)
        {
            return Result<IReadOnlyList<TopOrdersReportDto>>.Failure(
                new Error("ReportForbidden", "Only an active manager or admin can view this report."));
        }

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

        var managerId = actor.Role == UserRole.Manager
            ? actor.UserId
            : null;

        var orders = await repository.GetTopOrdersAsync(
            managerId,
            query.FromDate,
            query.ToDate,
            query.Limit,
            query.OnlyAccepted);

        return Result<IReadOnlyList<TopOrdersReportDto>>.Success(orders);
    }
}
