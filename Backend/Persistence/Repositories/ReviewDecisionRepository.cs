using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class ReviewDecisionRepository(AppDbContext context) : IReviewDecisionRepository
{
    public Task<TimeEntry?> GetForDecisionAsync(TimeEntryId entryId, CancellationToken cancellationToken = default) =>
        context.TimeEntries
            .AsTracking()
            .Include(entry => entry.Audits)
            .Include(entry => entry.Reviews)
            .AsSplitQuery()
            .SingleOrDefaultAsync(entry => entry.Id == entryId, cancellationToken);

    public Task<bool> HasReviewerTaskAccessAsync(
        EmployeeId reviewerId, TaskId taskId, CancellationToken cancellationToken = default) =>
        context.CatagoryReviewers.AsNoTracking().AnyAsync(reviewer => reviewer.EmployeeId == reviewerId &&
            context.Tasks.Any(task => task.Id == taskId && task.CatagoryId == reviewer.CatagoryId),
            cancellationToken);
}
