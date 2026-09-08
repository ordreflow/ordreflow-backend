using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Persistence;


namespace ViaPadel.Infrastructure.Repositories;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly AppDbContext _context;

    public TimeEntryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TimeEntry> CreateAsync(
        TimeEntry timeEntry,
        CancellationToken cancellationToken = default)
    {
        await _context.TimeEntries.AddAsync(
            timeEntry,
            cancellationToken);

        await _context.SaveChangesAsync(
            cancellationToken);

        return timeEntry;
    }
    
    public async Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var timeEntries = await _context.TimeEntries
            .Where(te => te.Date >= startDate && te.Date <= endDate)
            .ToListAsync(cancellationToken);

        return timeEntries;
    }

    
}