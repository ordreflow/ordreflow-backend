namespace Domain.Interfaces;

using Domain.Entities;
using Domain.Aggregate;

public interface ITimeEntryRepository
{
    
    //TODO: Change to fit the new structure, and add the generic structure for the repository
    Task<TimeEntry> CreateAsync(
        TimeEntry timeEntry,
        CancellationToken cancellationToken = default);
}