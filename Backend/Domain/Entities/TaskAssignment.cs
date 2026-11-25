using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class TaskAssignment
{
    public TaskId TaskId { get; set; } = null!;
    public EmployeeId AssignedTo { get; set; } = null!;
    public PlannedDuration ExpectedDuration { get; set; } = null!;
    public TargetCompletionDate ExpectedDeadline { get; set; } = null!;

    public static Result<TaskAssignment> Create(
        TaskId taskId,
        EmployeeId assignedTo,
        PlannedDuration expectedDuration,
        TargetCompletionDate expectedDeadline)
    {
        if (taskId is null || assignedTo is null || expectedDuration is null || expectedDeadline is null)
            return Result<TaskAssignment>.Failure(
                new Error("TaskAssignmentRequired", "Task, employee, expected duration and deadline are required."));

        return Result<TaskAssignment>.Success(new TaskAssignment
        {
            TaskId = taskId,
            AssignedTo = assignedTo,
            ExpectedDuration = expectedDuration,
            ExpectedDeadline = expectedDeadline
        });
    }
}
