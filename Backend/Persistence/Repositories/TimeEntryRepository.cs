
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class TimeEntryRepository(AppDbContext context)
    : GenericRepository<TimeEntry, TimeEntryId>(context),
        ITimeEntryRepository
{
    public Task<TimeEntry?> GetWithHistoryAsync(TimeEntryId id) =>
        Context.TimeEntries
            .Include(entry => entry.Audits)
            .Include(entry => entry.Reviews)
            .SingleOrDefaultAsync(entry => entry.Id == id);

    public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) =>
        GetWithHistoryAsync(id);

    public async Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(EmployeeId employeeId)
    {
        return await Context.TimeEntries
            .AsNoTracking()
            .Include(entry => entry.Audits)
            .Include(entry => entry.Reviews)
            .Where(entry => entry.EmployeeId == employeeId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(Guid managerId)
    {
        var typedManagerId = EmployeeId.Create(managerId);
        if (typedManagerId.IsFailure)
            return Array.Empty<TimeEntry>();

        var departmentIds = Context.DepartmentManagers
            .Where(item => item.ManagerId == typedManagerId.Value)
            .Select(item => item.DepartmentId);
        return await Context.TimeEntries
            .Include(entry => entry.Audits)
            .Join(
                Context.DepartmentEmployees,
                entry => entry.EmployeeId,
                membership => membership.EmployeeId,
                (entry, membership) => new { entry, membership })
            .Where(x => departmentIds.Contains(x.membership.DepartmentId))
            .Select(x => x.entry)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(
        DateTime from,
        DateTime to)
    {
        var entries = await Context.TimeEntries
            .Include(entry => entry.Audits)
            .ToListAsync();
        return entries
            .Where(entry => entry.GetCurrentAudit() is { } audit &&
                            audit.WorkDate.Value >= DateOnly.FromDateTime(from) &&
                            audit.WorkDate.Value < DateOnly.FromDateTime(to))
            .ToArray();
    }

    public async Task<bool> ExistsForTaskAsync(TaskId taskId)
    {
        return await Context.TimeEntryAudits
            .AnyAsync(audit => audit.TaskId == taskId);
    }
}
