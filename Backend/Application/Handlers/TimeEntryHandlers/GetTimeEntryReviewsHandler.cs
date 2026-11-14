using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetTimeEntryReviewsHandler(
    ITimeEntryRepository timeEntryRepository) : IQueryHandler<
        GetTimeEntryReviewsQuery,
        Result<IReadOnlyList<TimeEntryReviewDto>>>
{
    public async Task<Result<IReadOnlyList<TimeEntryReviewDto>>> HandleAsync(GetTimeEntryReviewsQuery query)
    {
        var actorIdResult = EmployeeId.Create(query.ActorId);
        var entryIdResult = TimeEntryId.Create(query.TimeEntryId);

        if (actorIdResult.IsFailure || entryIdResult.IsFailure)
            return Result<IReadOnlyList<TimeEntryReviewDto>>.Failure(
                (actorIdResult.IsFailure ? actorIdResult.Errors : entryIdResult.Errors).ToArray());

        var entry = await timeEntryRepository.GetWithHistoryAsync(entryIdResult.Value);

        if (entry is null)
            return Result<IReadOnlyList<TimeEntryReviewDto>>.Failure(
                new Error("TimeEntryNotFound", "The time entry was not found."));
        if (entry.EmployeeId != actorIdResult.Value)
            return Result<IReadOnlyList<TimeEntryReviewDto>>.Failure(
                new Error("TimeEntryAccessForbidden", "You are not authorized to view this time entry."));

        IReadOnlyList<TimeEntryReviewDto> reviews = entry.Reviews
            .OrderBy(review => review.ReviewedAtUtc)
            .ThenBy(review => review.Id.Value)
            .Select(review => new TimeEntryReviewDto(
                review.Id.Value,
                review.TimeEntryId.Value,
                review.Status.ToString(),
                review.Description?.Value,
                review.ReviewedAtUtc))
            .ToArray();

        return Result<IReadOnlyList<TimeEntryReviewDto>>.Success(reviews);
    }
}
