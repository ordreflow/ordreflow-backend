using Core.Tools.OperationResult;
using Domain.Aggregate;

namespace Domain.Services;

/// <summary>
/// Coordinates a time entry review decision (accept, return, or finalize),
/// which requires facts from both the employee's and the reviewer's User
/// aggregates together with the TimeEntry aggregate being reviewed.
/// </summary>
public sealed class TimeEntryReviewDomainService
{
    public Result Accept(TimeEntry entry, User? employee, User? reviewer)
    {
        if (employee is null || reviewer is null)
            return Result.Failure(new Error("UserNotFound", "The employee or reviewer was not found."));

        return entry.Accept(
            reviewer.UserId,
            employee.UserId,
            employee.ManagerId,
            reviewer.Role,
            reviewer.Status);
    }

    public Result Return(TimeEntry entry, User? employee, User? reviewer, string reason)
    {
        if (employee is null || reviewer is null)
            return Result.Failure(new Error("UserNotFound", "The employee or reviewer was not found."));

        return entry.Return(
            reviewer.UserId,
            employee.UserId,
            employee.ManagerId,
            reviewer.Role,
            reviewer.Status,
            reason);
    }

    public Result Finalize(TimeEntry entry, User? employee, User? reviewer)
    {
        if (employee is null || reviewer is null)
            return Result.Failure(new Error("UserNotFound", "The employee or reviewer was not found."));

        return entry.Finalize(
            reviewer.UserId,
            employee.UserId,
            employee.ManagerId,
            reviewer.Role,
            reviewer.Status);
    }
}
