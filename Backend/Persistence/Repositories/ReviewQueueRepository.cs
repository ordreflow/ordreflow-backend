using Domain.Aggregate;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class ReviewQueueRepository(AppDbContext context) : IReviewQueueRepository
{
    public async Task<IReadOnlyList<Guid>> GetAssignedCatagoryIdsAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
            return Array.Empty<Guid>();

        return await context.CatagoryReviewers
            .AsNoTracking()
            .Where(reviewer => reviewer.EmployeeId == employeeId)
            .Select(reviewer => reviewer.CatagoryId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetTaskIdsAsync(
        IReadOnlyCollection<Guid> catagoryIds, CancellationToken cancellationToken = default)
    {
        if (catagoryIds.Count == 0)
            return Array.Empty<Guid>();

        return await context.Tasks
            .AsNoTracking()
            .Where(task => catagoryIds.Contains(task.CatagoryId))
            .Select(task => task.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TimeEntry>> GetCandidatesWithAuditsAsync(
        IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0)
            return Array.Empty<TimeEntry>();

        var candidateIds = context.TimeEntryAudits
            .Where(audit => taskIds.Contains(audit.TaskId))
            .Select(audit => audit.TimeEntryId)
            .Distinct();

        // Include every version: filtering this navigation by task would make an
        // older authorized audit look current after the entry moves to another task.
        return await context.TimeEntries
            .AsNoTracking()
            .Where(entry => candidateIds.Contains(entry.Id))
            .Include(entry => entry.Audits)
            .ToListAsync(cancellationToken);
    }
}
