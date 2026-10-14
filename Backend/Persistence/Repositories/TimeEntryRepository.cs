
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class TimeEntryRepository(AppDbContext context)
    : GenericRepository<TimeEntry, TimeEntryId>(context),
        ITimeEntryRepository
{
    public async Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(
        UserId employeeId)
    {
        return await Context.TimeEntries
            .Where(entry => entry.EmployeeId == employeeId)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(
        UserId managerId)
    {
        return await Context.TimeEntries
            .Join(
                Context.Users,
                entry => entry.EmployeeId,
                employee => employee.UserId,
                (entry, employee) => new { entry, employee })
            .Where(x => x.employee.ManagerId == managerId)
            .Select(x => x.entry)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(
        DateTime from,
        DateTime to)
    {
        return await Context.TimeEntries
            .Where(entry =>
                entry.Date >= from &&
                entry.Date < to)
            .ToListAsync();
    }

    public async Task<bool> ExistsForTaskAsync(TaskId taskId)
    {
        return await Context.TimeEntries
            .AnyAsync(entry => entry.TaskId == taskId);
    }
}