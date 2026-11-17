using Domain.ValueObjects;

namespace Application.Interfaces;

/// <summary>
/// One work item the employee may select, with its category and department for context. Only the employee's own
/// assignment details are included; other assignees and hidden sibling work are never returned (FR-21, USE-04).
/// </summary>
public sealed record AccessibleWorkDto(
    Guid Id,
    string Name,
    string? Description,
    string Kind,
    bool AssignmentOnly,
    bool AssignedToMe,
    decimal? ExpectedHours,
    DateOnly? ExpectedDeadline,
    Guid CategoryId,
    string CategoryName,
    Guid DepartmentId,
    string DepartmentName);

/// <summary>The work an employee may select, filtered in the database (FR-09, PERF-01).</summary>
public interface IAccessibleWorkQuery
{
    Task<IReadOnlyList<AccessibleWorkDto>> ListAsync(EmployeeId employee, bool assignedToMeOnly, CancellationToken ct = default);

    Task<AccessibleWorkDto?> GetAsync(EmployeeId employee, TaskId task, CancellationToken ct = default);
}
