using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IReviewQueueRepository
{
    // The caller must verify the employee's active reviewer permission before
    // invoking any of these lookups.
    Task<IReadOnlyList<CatagoryId>> GetAssignedCatagoryIdsAsync(
        EmployeeId employeeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskId>> GetTaskIdsAsync(
        IReadOnlyCollection<CatagoryId> catagoryIds, CancellationToken cancellationToken = default);

    // Candidates can have an older audit on an assigned task. The caller must check
    // GetCurrentAudit().TaskId against the authorized task IDs before returning them.
    Task<IReadOnlyList<TimeEntry>> GetCandidatesWithAuditsAsync(
        IReadOnlyCollection<TaskId> taskIds, CancellationToken cancellationToken = default);
}
