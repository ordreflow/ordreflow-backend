namespace Persistence;

using Domain.Aggregate;
using Domain.Interfaces.IUnitOfWork;
using Microsoft.EntityFrameworkCore;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException error) when (error.Entries.Any(entry => entry.Entity is TimeEntry))
        {
            throw new ConcurrentTimeEntryException(error);
        }
    }
}
