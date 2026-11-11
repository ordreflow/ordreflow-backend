using Core.Tools.OperationResult;
using Domain.ValueObjects;
using WorkAssignmentEntity = Domain.Entities.WorkAssignment;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Domain.Entities;

public sealed class Task
{
    private readonly List<Task> _children = new();
    private readonly List<WorkAssignmentEntity> _assignments = new();

    public TaskId Id { get; private set; } = null!;
    public OrderId OrderId { get; private set; } = null!;
    public TaskId? ParentTaskId { get; private set; }
    public string Title { get; private set; } = null!;
    public TaskKind Kind { get; private set; }
    public bool AssignmentOnly { get; private set; }
    public TaskStatus Status { get; private set; }
    public IReadOnlyCollection<Task> Children => _children.AsReadOnly();
    public IReadOnlyCollection<WorkAssignmentEntity> Assignments => _assignments.AsReadOnly();

    private Task()
    {
    }

    private Task(
        OrderId orderId,
        TaskId? parentTaskId,
        string title,
        TaskKind kind,
        bool assignmentOnly)
    {
        Id = TaskId.Create(Guid.NewGuid()).Value;
        OrderId = orderId;
        ParentTaskId = parentTaskId;
        Title = title;
        Kind = kind;
        AssignmentOnly = assignmentOnly;
        Status = TaskStatus.Open;
    }

    internal static Result<Task> CreateRoot(
        OrderId orderId,
        string title,
        TaskKind kind,
        bool assignmentOnly)
    {
        if (orderId is null)
            return Result<Task>.Failure(new Error("OrderRequired", "A task must belong to an order."));

        var normalizedTitle = NormalizeTitle(title);
        if (normalizedTitle.IsFailure)
            return Result<Task>.Failure(normalizedTitle.Errors.ToArray());

        if (!Enum.IsDefined(kind))
            return Result<Task>.Failure(new Error("InvalidTaskKind", "The task kind is invalid."));

        if (assignmentOnly && kind != TaskKind.Work)
            return Result<Task>.Failure(new Error("GroupTaskCannotBeAssignmentOnly", "Only work tasks can require an assignment."));

        return Result<Task>.Success(new Task(orderId, null, normalizedTitle.Value, kind, assignmentOnly));
    }

    public Result<Task> CreateChild(string title, TaskKind kind, bool assignmentOnly)
    {
        if (Status is TaskStatus.Finished or TaskStatus.Cancelled)
            return Result<Task>.Failure(new Error("ParentTaskInactive", "A child cannot be added to an inactive task."));

        var normalizedTitle = NormalizeTitle(title);
        if (normalizedTitle.IsFailure)
            return Result<Task>.Failure(normalizedTitle.Errors.ToArray());

        if (!Enum.IsDefined(kind))
            return Result<Task>.Failure(new Error("InvalidTaskKind", "The task kind is invalid."));

        if (assignmentOnly && kind != TaskKind.Work)
            return Result<Task>.Failure(new Error("GroupTaskCannotBeAssignmentOnly", "Only work tasks can require an assignment."));

        var child = new Task(OrderId, Id, normalizedTitle.Value, kind, assignmentOnly);
        _children.Add(child);
        return Result<Task>.Success(child);
    }

    public Result AddAssignment(WorkAssignmentEntity assignment)
    {
        if (assignment is null)
            return Result.Failure(new Error("WorkAssignmentRequired", "A work assignment is required."));

        if (assignment.TaskId != Id)
            return Result.Failure(new Error("WorkAssignmentMismatch", "The assignment belongs to another task."));

        if (Status is TaskStatus.Finished or TaskStatus.Cancelled)
            return Result.Failure(new Error("TaskInactive", "An inactive task cannot receive assignments."));

        if (Kind != TaskKind.Work)
            return Result.Failure(new Error("GroupTaskCannotBeAssigned", "Assignments can only be added to work tasks."));

        if (_assignments.Any(existing => existing.UserId == assignment.UserId &&
            existing.Status == WorkAssignmentStatus.Active))
            return Result.Failure(new Error("ActiveAssignmentExists", "This user already has an active assignment for the task."));

        _assignments.Add(assignment);
        Status = TaskStatus.InProgress;
        return Result.Success();
    }

    public bool HasActiveAssignment(UserId userId) =>
        userId is not null &&
        _assignments.Any(assignment =>
            assignment.UserId == userId &&
            assignment.Status == WorkAssignmentStatus.Active);

    public bool CanView(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        DepartmentId departmentId,
        DepartmentStatus departmentStatus,
        OrderId orderId,
        bool orderVisibleInDepartment)
    {
        if (actorId is null ||
            actorStatus != UserStatus.Active ||
            departmentId is null ||
            departmentStatus != DepartmentStatus.Active ||
            OrderId != orderId ||
            !orderVisibleInDepartment)
            return false;

        if (actorRole == UserRole.Admin)
            return true;

        if (actorDepartmentId != departmentId)
            return false;

        if (actorRole == UserRole.Manager)
            return true;

        if (actorRole != UserRole.Employee)
            return false;

        var activeAssignments = _assignments
            .Where(assignment => assignment.Status == WorkAssignmentStatus.Active)
            .ToArray();

        return activeAssignments.Length == 0 ||
            activeAssignments.Any(assignment => assignment.UserId == actorId);
    }

    public bool Contains(TaskId taskId) =>
        taskId is not null &&
        (Id == taskId || _children.Any(child => child.Contains(taskId)));

    public Result MarkFinished()
    {
        if (Status == TaskStatus.Finished)
            return Result.Failure(new Error("TaskAlreadyFinished", "The task is already finished."));

        if (Status == TaskStatus.Cancelled)
            return Result.Failure(new Error("TaskCancelled", "A cancelled task cannot be finished."));

        if (_children.Any(child => child.Status is not (TaskStatus.Finished or TaskStatus.Cancelled)))
            return Result.Failure(new Error("OpenChildTasks", "All child tasks must be finished or cancelled first."));

        if (AssignmentOnly &&
            !_assignments.Any(assignment => assignment.Status == WorkAssignmentStatus.Finished))
            return Result.Failure(new Error("AssignmentRequired", "An assignment-only task requires a finished assignment."));

        if (_assignments.Any(assignment => assignment.Status == WorkAssignmentStatus.Active))
            return Result.Failure(new Error("ActiveAssignmentsRemain", "All active assignments must be finished or cancelled first."));

        Status = TaskStatus.Finished;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == TaskStatus.Finished)
            return Result.Failure(new Error("TaskAlreadyFinished", "A finished task cannot be cancelled."));

        if (Status == TaskStatus.Cancelled)
            return Result.Failure(new Error("TaskAlreadyCancelled", "The task is already cancelled."));

        Status = TaskStatus.Cancelled;
        return Result.Success();
    }

    private static Result<string> NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<string>.Failure(new Error("TaskTitleRequired", "Task title is required."));

        title = title.Trim();
        return title.Length > 200
            ? Result<string>.Failure(new Error("TaskTitleTooLong", "Task title cannot exceed 200 characters."))
            : Result<string>.Success(title);
    }
}
