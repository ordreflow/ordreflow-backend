using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class TimeEntryAccessGrant
{
    public TimeEntryAccessGrantId Id { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public TimeEntryAccessAction AllowedActions { get; private set; }
    public DateOnly? WorkDateFrom { get; private set; }
    public DateOnly? WorkDateTo { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool Revoked { get; private set; }

    private TimeEntryAccessGrant()
    {
    }

    private TimeEntryAccessGrant(
        UserId userId,
        TimeEntryAccessAction allowedActions,
        DateOnly? workDateFrom,
        DateOnly? workDateTo,
        DateTime? expiresAt)
    {
        Id = TimeEntryAccessGrantId.Create(Guid.NewGuid()).Value;
        UserId = userId;
        AllowedActions = allowedActions;
        WorkDateFrom = workDateFrom;
        WorkDateTo = workDateTo;
        ExpiresAt = expiresAt;
    }

    public static Result<TimeEntryAccessGrant> Create(
        UserId userId,
        TimeEntryAccessAction allowedActions,
        DateOnly? workDateFrom,
        DateOnly? workDateTo,
        DateTime? expiresAt)
    {
        if (userId is null)
            return Result<TimeEntryAccessGrant>.Failure(new Error("UserRequired", "A user is required."));

        if (allowedActions == TimeEntryAccessAction.None ||
            (allowedActions & ~(TimeEntryAccessAction.View | TimeEntryAccessAction.Create |
                TimeEntryAccessAction.Correct | TimeEntryAccessAction.Review |
                TimeEntryAccessAction.Export)) != 0)
            return Result<TimeEntryAccessGrant>.Failure(new Error("InvalidAccessActions", "At least one valid access action is required."));

        if (workDateFrom.HasValue && workDateTo.HasValue && workDateFrom > workDateTo)
            return Result<TimeEntryAccessGrant>.Failure(new Error("InvalidWorkDateRange", "The start date must be on or before the end date."));

        return Result<TimeEntryAccessGrant>.Success(new TimeEntryAccessGrant(
            userId,
            allowedActions,
            workDateFrom,
            workDateTo,
            expiresAt));
    }

    public bool Allows(
        TimeEntryAccessAction action,
        DateOnly workDate,
        DateTime utcNow)
    {
        const TimeEntryAccessAction allActions =
            TimeEntryAccessAction.View |
            TimeEntryAccessAction.Create |
            TimeEntryAccessAction.Correct |
            TimeEntryAccessAction.Review |
            TimeEntryAccessAction.Export;

        if (action == TimeEntryAccessAction.None || (action & ~allActions) != 0)
            return false;

        if (Revoked || (AllowedActions & action) != action)
            return false;

        if (ExpiresAt.HasValue && utcNow >= ExpiresAt.Value)
            return false;

        return (!WorkDateFrom.HasValue || workDate >= WorkDateFrom.Value) &&
            (!WorkDateTo.HasValue || workDate <= WorkDateTo.Value);
    }

    public Result Revoke()
    {
        if (Revoked)
            return Result.Failure(new Error("AccessGrantAlreadyRevoked", "The access grant is already revoked."));

        Revoked = true;
        return Result.Success();
    }
}
