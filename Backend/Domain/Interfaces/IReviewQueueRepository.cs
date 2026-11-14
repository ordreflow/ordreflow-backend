using Domain.Aggregate;

namespace Domain.Interfaces;

public interface IReviewQueueRepository
{
    // The caller must verify the employee's active reviewer permission before
    // invoking any of these lookups.
    Task<IReadOnlyList<Guid>> GetAssignedCatagoryIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetTaskIdsAsync(
        IReadOnlyCollection<Guid> catagoryIds, CancellationToken cancellationToken = default);

    // Candidates can have an older audit on an assigned task. The caller must check
    // GetCurrentAudit().TaskId against the authorized task IDs before returning them.
    Task<IReadOnlyList<TimeEntry>> GetCandidatesWithAuditsAsync(
        IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken = default);
}
