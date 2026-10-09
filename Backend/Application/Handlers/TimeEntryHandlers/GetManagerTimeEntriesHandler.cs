using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetManagerTimeEntriesHandler(
    ITimeEntryRepository repository,
    IUserRepository userRepository) : IQueryHandler<
        GetManagerTimeEntriesQuery,
        Result<IReadOnlyList<TimeEntryDto>>>
{
    public async Task<Result<IReadOnlyList<TimeEntryDto>>> HandleAsync(
        GetManagerTimeEntriesQuery query)
    {
        var managerIdResult = UserId.Create(query.ManagerId);

        if (managerIdResult.IsFailure)
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(managerIdResult.Errors.ToArray());

        var manager = await userRepository.GetAsync(managerIdResult.Value);
        if (manager is null)
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(
                new Error("ManagerNotFound", "The manager was not found."));

        if (!manager.CanReviewTimeEntries)
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(
                new Error("ReviewForbidden", "Only an active manager or admin can view time entries."));

        var entries = await repository.GetByManagerIdAsync(managerIdResult.Value);
        var filtered = entries
            .Where(entry => !query.FromDate.HasValue || entry.Date >= query.FromDate.Value)
            .Where(entry => !query.ToDateExclusive.HasValue || entry.Date < query.ToDateExclusive.Value)
            .Where(entry => !query.Status.HasValue || entry.Status == query.Status.Value)
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
