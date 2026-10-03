using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface ITimeEntryRepository
    : IGenericRepository<TimeEntry, TimeEntryId>
{
    Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(
        UserId employeeId);

    Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(
        UserId managerId);

    Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(
        DateTime from,
        DateTime to);
}