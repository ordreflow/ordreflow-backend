using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public sealed record ReviewTaskNames(string TaskName, string CategoryName, string DepartmentName);

public sealed record ReviewDisplayNames(
    IReadOnlyDictionary<Guid, string> Employees,
    IReadOnlyDictionary<Guid, ReviewTaskNames> Tasks);

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

    // Display names for entries the caller has already authorized for the reviewer.
    Task<ReviewDisplayNames> GetDisplayNamesAsync(
        IReadOnlyCollection<EmployeeId> employeeIds, IReadOnlyCollection<TaskId> taskIds,
        CancellationToken cancellationToken = default);
}
