using Core.Tools.OperationResult;
using Domain.ValueObjects;
using WorkTask = Domain.Entities.Task;

namespace Domain.Aggregate;

public sealed class Order
{
    private readonly List<WorkTask> _rootTasks = new();
    private readonly HashSet<DepartmentId> _visibleInDepartments = new();

    public OrderId Id { get; private set; } = null!;
    public OrderId? ParentOrderId { get; private set; }
    public OrderName Name { get; private set; } = null!;
    public DepartmentId ChargedDepartmentId { get; private set; } = null!;
    public OrderVisibility Visibility { get; private set; }
    public IReadOnlyCollection<DepartmentId> VisibleInDepartments => _visibleInDepartments.ToArray();
    public IReadOnlyCollection<WorkTask> RootTasks => _rootTasks.AsReadOnly();

    private Order()
    {
    }

    private Order(
        OrderName name,
        OrderId? parentOrderId,
        DepartmentId chargedDepartmentId,
        OrderVisibility visibility,
        IEnumerable<DepartmentId> visibleInDepartments)
    {
        Id = OrderId.Create(Guid.NewGuid()).Value;
        ParentOrderId = parentOrderId;
        Name = name;
        ChargedDepartmentId = chargedDepartmentId;
        Visibility = visibility;
        _visibleInDepartments.UnionWith(visibleInDepartments);
    }

    public static Result<Order> Create(
        string name,
        OrderId? parentOrderId,
        DepartmentId chargedDepartmentId,
        DepartmentStatus departmentStatus,
        DepartmentId? parentChargedDepartmentId,
        OrderVisibility visibility,
        IEnumerable<DepartmentId>? visibleInDepartments = null)
    {
        if (chargedDepartmentId is null || departmentStatus != DepartmentStatus.Active)
            return Result<Order>.Failure(new Error("ChargedDepartmentUnavailable", "An active charged department is required."));

        if (!Enum.IsDefined(visibility))
            return Result<Order>.Failure(new Error("InvalidOrderVisibility", "Order visibility is invalid."));

        if (parentOrderId is not null && parentChargedDepartmentId != chargedDepartmentId)
            return Result<Order>.Failure(new Error("ParentOrderDepartmentMismatch", "A child order must use its parent's charged department."));

        var orderName = OrderName.Create(name);
        if (orderName.IsFailure)
            return Result<Order>.Failure(orderName.Errors.ToArray());

        var visibleDepartments = visibleInDepartments?.ToArray() ?? Array.Empty<DepartmentId>();
        if (visibleDepartments.Any(departmentId => departmentId is null))
            return Result<Order>.Failure(new Error("InvalidVisibleDepartment", "Visible departments cannot contain null values."));

        if (visibility == OrderVisibility.SelectedDepartments && visibleDepartments.Length == 0)
            return Result<Order>.Failure(new Error("VisibleDepartmentRequired", "At least one visible department is required for selected-department visibility."));

        if (visibility != OrderVisibility.SelectedDepartments && visibleDepartments.Length > 0)
            return Result<Order>.Failure(new Error("UnexpectedVisibleDepartments", "Explicit visible departments are only valid for selected-department visibility."));

        return Result<Order>.Success(new Order(
            orderName.Value,
            parentOrderId,
            chargedDepartmentId,
            visibility,
            visibleDepartments));
    }

    public bool IsVisibleIn(DepartmentId departmentId)
    {
        if (departmentId is null)
            return false;

        return Visibility switch
        {
            OrderVisibility.AllDepartments => true,
            OrderVisibility.SelectedDepartments => _visibleInDepartments.Contains(departmentId),
            OrderVisibility.ChargedDepartment => ChargedDepartmentId == departmentId,
            _ => false
        };
    }

    public bool CanView(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        DepartmentId departmentId,
        DepartmentStatus departmentStatus)
    {
        if (actorId is null || actorStatus != UserStatus.Active ||
            departmentId is null || departmentStatus != DepartmentStatus.Active)
            return false;

        if (actorRole == UserRole.Admin)
            return true;

        return actorDepartmentId == departmentId &&
            actorRole is UserRole.Employee or UserRole.Manager &&
            IsVisibleIn(departmentId);
    }

    public Result Rename(string name)
    {
        var orderName = OrderName.Create(name);
        if (orderName.IsFailure)
            return Result.Failure(orderName.Errors.ToArray());

        Name = orderName.Value;
        return Result.Success();
    }

    public Result ChangeVisibility(
        OrderVisibility visibility,
        IEnumerable<DepartmentId>? visibleInDepartments = null)
    {
        if (!Enum.IsDefined(visibility))
            return Result.Failure(new Error("InvalidOrderVisibility", "Order visibility is invalid."));

        var visibleDepartments = visibleInDepartments?.ToArray() ?? Array.Empty<DepartmentId>();
        if (visibleDepartments.Any(departmentId => departmentId is null))
            return Result.Failure(new Error("InvalidVisibleDepartment", "Visible departments cannot contain null values."));

        if (visibility == OrderVisibility.SelectedDepartments && visibleDepartments.Length == 0)
            return Result.Failure(new Error("VisibleDepartmentRequired", "At least one visible department is required for selected-department visibility."));

        if (visibility != OrderVisibility.SelectedDepartments && visibleDepartments.Length > 0)
            return Result.Failure(new Error("UnexpectedVisibleDepartments", "Explicit visible departments are only valid for selected-department visibility."));

        Visibility = visibility;
        _visibleInDepartments.Clear();
        _visibleInDepartments.UnionWith(visibleDepartments);
        return Result.Success();
    }

    private Result<WorkTask> CreateRootTask(string title, TaskKind kind, bool assignmentOnly)
    {
        var task = WorkTask.CreateRoot(Id, title, kind, assignmentOnly);
        if (task.IsFailure)
            return task;

        _rootTasks.Add(task.Value);
        return task;
    }

    public Result<WorkTask> CreateRootTask(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        DepartmentId departmentId,
        DepartmentStatus departmentStatus,
        string title,
        TaskKind kind,
        bool assignmentOnly)
    {
        if (actorId is null || actorStatus != UserStatus.Active ||
            actorRole is not (UserRole.Manager or UserRole.Admin))
            return Result<WorkTask>.Failure(new Error("TaskCreationForbidden", "Only an active manager or admin can create tasks."));

        if (departmentId is null || departmentStatus != DepartmentStatus.Active ||
            departmentId != ChargedDepartmentId)
            return Result<WorkTask>.Failure(new Error("DepartmentUnavailable", "The order's active charged department is required."));

        if (actorRole == UserRole.Manager && actorDepartmentId != departmentId)
            return Result<WorkTask>.Failure(new Error("DepartmentScopeForbidden", "A manager can only manage tasks in their own department."));

        return CreateRootTask(title, kind, assignmentOnly);
    }

    public bool ContainsTask(TaskId taskId) =>
        taskId is not null &&
        _rootTasks.Any(root => root.Contains(taskId));

}
