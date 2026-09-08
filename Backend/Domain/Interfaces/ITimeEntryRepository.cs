namespace Domain.Interfaces;

using Domain.Entities;


public interface ITimeEntryRepository
{
    Task<TimeEntry> CreateAsync(
        TimeEntry timeEntry,
        CancellationToken cancellationToken = default);

   //get time entries by user id and date range
    Task<IEnumerable<TimeEntry>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}