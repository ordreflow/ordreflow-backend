namespace Domain.Interfaces;

using Domain.Entities;


public interface ITimeEntryRepository
{
    Task<TimeEntry> CreateAsync(
        TimeEntry timeEntry,
        CancellationToken cancellationToken = default);
}