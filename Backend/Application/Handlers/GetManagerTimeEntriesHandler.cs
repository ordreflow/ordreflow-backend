using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;

namespace Application.Handlers;

public sealed class GetManagerTimeEntriesHandler(
    ITimeEntryRepository repository,
    IUserRepository userRepository) : IQueryHandler<
        GetManagerTimeEntriesQuery,
        Result<IReadOnlyList<TimeEntry>>>
{
    public async Task<Result<IReadOnlyList<TimeEntry>>> HandleAsync(
        GetManagerTimeEntriesQuery query)
    {
        var manager = await userRepository.GetAsync(query.ManagerId);
        if (manager is null)
            return Result<IReadOnlyList<TimeEntry>>.Failure(
                new Error("ManagerNotFound", "The manager was not found."));

        if (manager.Status != Domain.ValueObjects.UserStatus.Active ||
            manager.Role is not (Domain.ValueObjects.UserRole.Manager or Domain.ValueObjects.UserRole.Admin))
            return Result<IReadOnlyList<TimeEntry>>.Failure(
                new Error("ReviewForbidden", "Only an active manager or admin can view time entries."));

        var entries = await repository.GetByManagerIdAsync(query.ManagerId);
        var filtered = entries
            .Where(entry => !query.FromDate.HasValue || entry.Date >= query.FromDate.Value)
            .Where(entry => !query.ToDateExclusive.HasValue || entry.Date < query.ToDateExclusive.Value)
            .Where(entry => !query.Status.HasValue || entry.Status == query.Status.Value)
            .OrderBy(entry => entry.Date)
            .ToArray();

        return Result<IReadOnlyList<TimeEntry>>.Success(filtered);
    }
}
