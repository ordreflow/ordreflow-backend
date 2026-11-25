using Domain.Entities;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;
using WorkTask = Domain.Entities.Task;

namespace Domain.Services;

/// <summary>
/// Which work an employee may find and select (FR-09, FR-21, SEC-02). It mirrors the rules that
/// <see cref="TimeRegistrationDomainService"/> applies when time is registered, so the list never shows work the
/// employee cannot use and never hides work they can: the category and task are active, work is scoped to its
/// department except for its assignees, internal-only categories require department membership, and assigned work
/// is only for its assignees.
/// </summary>
public static class WorkSelectionRules
{
    public static bool IsSelectable(Catagory catagory, WorkTask task, bool isDepartmentMember, bool hasAssignments, bool isAssigned) =>
        catagory is not null && task is not null &&
        catagory.Status == CatagoryStatus.Active &&
        task.Status == TaskStatus.Active &&
        (!catagory.InternalOnly || isDepartmentMember) &&
        (isDepartmentMember || isAssigned) &&
        (isAssigned || (!task.AssignmentOnly && !hasAssignments));
}
