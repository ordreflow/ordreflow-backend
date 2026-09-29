
using Domain.Aggregate;
using Domain.Interfaces;

using Persistence;


//TODO: This configuration is commented out because it need to be changed to fit the new structure


namespace ViaPadel.Infrastructure.Repositories;
/*
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
      //not implemented yet
      throw new NotImplementedException();
    }
}*/