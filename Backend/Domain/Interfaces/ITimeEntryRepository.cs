using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Interfaces;

public interface ITimeEntryRepository
    : IGenericRepository<TimeEntry, TimeEntryId>
{
    Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id);

    Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(
        UserId employeeId);

    Task<IReadOnlyList<TimeEntry>> GetByDepartmentIdAsync(
        DepartmentId departmentId);

    Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(
        DateTime from,
        DateTime to);

    Task<bool> ExistsForTaskAsync(TaskId taskId);
}
