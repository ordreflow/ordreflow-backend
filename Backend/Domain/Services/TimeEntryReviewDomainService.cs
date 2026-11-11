using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
/// Coordinates a time entry review decision (accept, return, or finalize),
/// which requires facts from both the employee's and the reviewer's User
/// aggregates together with the TimeEntry aggregate being reviewed.
/// </summary>
public sealed class TimeEntryReviewDomainService
{
    public Result Accept(
        TimeEntry entry,
        UserId employeeId,
        DepartmentId? employeeDepartmentId,
        UserId reviewerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus,
        DepartmentId? reviewerDepartmentId)
    {
        if (entry is null)
            return Result.Failure(new Error("TimeEntryRequired", "A time entry is required."));

        return entry.Accept(
            reviewerId,
            reviewerRole,
            reviewerStatus,
            reviewerDepartmentId,
            employeeId,
            employeeDepartmentId);
    }

    public Result Return(
        TimeEntry entry,
        UserId employeeId,
        DepartmentId? employeeDepartmentId,
        UserId reviewerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus,
        DepartmentId? reviewerDepartmentId,
        string reason)
    {
        if (entry is null)
            return Result.Failure(new Error("TimeEntryRequired", "A time entry is required."));

        return entry.Return(
            reviewerId,
            reviewerRole,
            reviewerStatus,
            reviewerDepartmentId,
            employeeId,
            employeeDepartmentId,
            reason);
    }

    public Result Finalize(
        TimeEntry entry,
        UserId employeeId,
        DepartmentId? employeeDepartmentId,
        UserId reviewerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus,
        DepartmentId? reviewerDepartmentId)
    {
        if (entry is null)
            return Result.Failure(new Error("TimeEntryRequired", "A time entry is required."));

        return entry.Finalize(
            reviewerId,
            reviewerRole,
            reviewerStatus,
            reviewerDepartmentId,
            employeeId,
            employeeDepartmentId);
    }
}
