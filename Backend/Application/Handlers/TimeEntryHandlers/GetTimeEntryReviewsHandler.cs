using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetTimeEntryReviewsHandler(
    ITimeEntryRepository timeEntryRepository,
    IUserRepository userRepository) : IQueryHandler<
        GetTimeEntryReviewsQuery,
        Result<IReadOnlyList<TimeEntryReviewDto>>>
{
    public async Task<Result<IReadOnlyList<TimeEntryReviewDto>>> HandleAsync(GetTimeEntryReviewsQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
            return Result<IReadOnlyList<TimeEntryReviewDto>>.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
            return Result<IReadOnlyList<TimeEntryReviewDto>>.Failure(
                new Error("UserNotFound", "The current user could not be found."));

        var entryIdResult = TimeEntryId.Create(query.TimeEntryId);

        if (entryIdResult.IsFailure)
            return Result<IReadOnlyList<TimeEntryReviewDto>>.Failure(entryIdResult.Errors.ToArray());

        var entry = await timeEntryRepository.GetWithReviewsAsync(entryIdResult.Value);

        if (entry is null)
            return Result<IReadOnlyList<TimeEntryReviewDto>>.Failure(
                new Error("TimeEntryNotFound", "The time entry was not found."));

        var employee = await userRepository.GetAsync(entry.EmployeeId);

        if (!entry.CanView(actor.UserId, actor.Role, actor.Status, employee?.ManagerId))
            return Result<IReadOnlyList<TimeEntryReviewDto>>.Failure(
                new Error("TimeEntryAccessForbidden", "You are not authorized to view this time entry."));

        IReadOnlyList<TimeEntryReviewDto> reviews = entry.Reviews
            .OrderBy(review => review.ReviewedAt)
            .ThenBy(review => review.Id.Value)
            .Select(review => new TimeEntryReviewDto(
                review.Id.Value,
                review.TimeEntryId.Value,
                review.Decision.ToString(),
                review.Reason,
                review.ReviewedAt))
            .ToArray();

        return Result<IReadOnlyList<TimeEntryReviewDto>>.Success(reviews);
    }
}
