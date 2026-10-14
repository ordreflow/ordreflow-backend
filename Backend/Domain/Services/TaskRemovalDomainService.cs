using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
/// Coordinates the cross-aggregate rule that a task cannot be removed from
/// its order while time entries still reference it, preserving historical
/// time registrations.
/// </summary>
public sealed class TaskRemovalDomainService
{
    public Result RemoveTask(
        Order order,
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        TaskId taskId,
        bool taskHasTimeEntries)
    {
        if (order is null)
            return Result.Failure(
                new Error(
                    "OrderRequired",
                    "An order is required."));

        if (taskHasTimeEntries)
            return Result.Failure(
                new Error(
                    "TaskHasTimeEntries",
                    "The task cannot be deleted because time entries reference it."));

        return order.RemoveWorkItem(actorId, actorRole, actorStatus, taskId);
    }
}
