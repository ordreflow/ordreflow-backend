using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface ITimeEntryRepository : IGenericRepository<TimeEntry, TimeEntryId>
{
    Task<TimeEntry?> GetWithHistoryAsync(TimeEntryId id);
    Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id);
    Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(EmployeeId employeeId);
    Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(Guid managerId);
    Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<bool> ExistsForTaskAsync(TaskId taskId);
}
