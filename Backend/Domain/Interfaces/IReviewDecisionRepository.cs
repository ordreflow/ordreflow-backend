using Domain.Aggregate;

namespace Domain.Interfaces;

public interface IReviewDecisionRepository
{
    Task<TimeEntry?> GetForDecisionAsync(Guid entryId, CancellationToken cancellationToken = default);
    Task<bool> HasReviewerTaskAccessAsync(Guid reviewerId, Guid taskId, CancellationToken cancellationToken = default);
}
