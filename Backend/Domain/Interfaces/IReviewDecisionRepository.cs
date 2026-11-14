using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface IReviewDecisionRepository
{
    Task<TimeEntry?> GetForDecisionAsync(TimeEntryId entryId, CancellationToken cancellationToken = default);
    Task<bool> HasReviewerTaskAccessAsync(
        EmployeeId reviewerId,
        TaskId taskId,
        CancellationToken cancellationToken = default);
}
