using Domain.Aggregate;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class ReviewDecisionRepository(AppDbContext context) : IReviewDecisionRepository
{
    public Task<TimeEntry?> GetForDecisionAsync(Guid entryId, CancellationToken cancellationToken = default) =>
        context.TimeEntries
            .AsTracking()
            .Include(entry => entry.Audits)
            .Include(entry => entry.Reviews)
            .AsSplitQuery()
            .SingleOrDefaultAsync(entry => entry.Id == entryId, cancellationToken);

    public Task<bool> HasReviewerTaskAccessAsync(
        Guid reviewerId, Guid taskId, CancellationToken cancellationToken = default) =>
        context.CatagoryReviewers.AsNoTracking().AnyAsync(reviewer => reviewer.EmployeeId == reviewerId &&
            context.Tasks.Any(task => task.Id == taskId && task.CatagoryId == reviewer.CatagoryId),
            cancellationToken);
}
