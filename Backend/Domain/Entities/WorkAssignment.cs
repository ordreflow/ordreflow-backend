using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class WorkAssignment
{
    public WorkAssignmentId Id { get; private set; } = null!;
    public TaskId TaskId { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public int ExpectedMinutes { get; private set; }
    public DateOnly? TargetCompletionDate { get; private set; }
    public WorkAssignmentStatus Status { get; private set; }

    private WorkAssignment()
    {
    }

    private WorkAssignment(
        TaskId taskId,
        UserId userId,
        int expectedMinutes,
        DateOnly? targetCompletionDate)
    {
        Id = WorkAssignmentId.Create(Guid.NewGuid()).Value;
        TaskId = taskId;
        UserId = userId;
        ExpectedMinutes = expectedMinutes;
        TargetCompletionDate = targetCompletionDate;
        Status = WorkAssignmentStatus.Active;
    }

    public static Result<WorkAssignment> Create(
        TaskId taskId,
        UserId userId,
        int expectedMinutes,
        DateOnly? targetCompletionDate)
    {
        if (taskId is null)
            return Result<WorkAssignment>.Failure(new Error("TaskRequired", "A task is required."));

        if (userId is null)
            return Result<WorkAssignment>.Failure(new Error("UserRequired", "An assigned user is required."));

        if (expectedMinutes <= 0)
            return Result<WorkAssignment>.Failure(new Error("InvalidExpectedMinutes", "Expected minutes must be greater than zero."));

        return Result<WorkAssignment>.Success(new WorkAssignment(
            taskId,
            userId,
            expectedMinutes,
            targetCompletionDate));
    }

    public Result UpdatePlan(int expectedMinutes, DateOnly? targetCompletionDate)
    {
        if (Status != WorkAssignmentStatus.Active)
            return Result.Failure(new Error("AssignmentInactive", "Only an active assignment can be changed."));

        if (expectedMinutes <= 0)
            return Result.Failure(new Error("InvalidExpectedMinutes", "Expected minutes must be greater than zero."));

        ExpectedMinutes = expectedMinutes;
        TargetCompletionDate = targetCompletionDate;
        return Result.Success();
    }

    public Result MarkFinished()
    {
        if (Status != WorkAssignmentStatus.Active)
            return Result.Failure(new Error("AssignmentInactive", "Only an active assignment can be finished."));

        Status = WorkAssignmentStatus.Finished;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != WorkAssignmentStatus.Active)
            return Result.Failure(new Error("AssignmentInactive", "Only an active assignment can be cancelled."));

        Status = WorkAssignmentStatus.Cancelled;
        return Result.Success();
    }
}
