using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;
using Task = Domain.Entities.Task;

namespace Domain.Aggregate;

public sealed class Order
{
	private readonly List<Task> _tasks = new();

	public OrderId Id { get; private set; } = null!;
	public UserId ManagerId { get; private set; } = null!;
	public OrderName Name { get; private set; } = null!;
	public OrderStatus Status { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime? ClosedAt { get; private set; }
	public IReadOnlyCollection<Task> Tasks => _tasks.AsReadOnly();

	private Order()
	{
	}

	private Order(UserId managerId, OrderName name)
	{
		Id = OrderId.Create(Guid.NewGuid()).Value;
		ManagerId = managerId;
		Name = name;
		Status = OrderStatus.Open;
		CreatedAt = DateTime.UtcNow;
	}

	public static Result<Order> Create(
		UserId managerId,
		UserRole managerRole,
		UserStatus managerStatus,
		OrderName name)
	{
		if (managerId is null || managerStatus != UserStatus.Active ||
			(managerRole is not (UserRole.Manager or UserRole.Admin)))
			return Result<Order>.Failure(new Error("CaseCreationForbidden", "Only an active manager or admin can create cases."));

		if (name is null)
			return Result<Order>.Failure(new Error("CaseNameRequired", "OrderCommands name is required."));

		return Result<Order>.Success(new Order(managerId, name));
	}

	public Result Rename(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus,
		OrderName name)
	{
		if (!CanManage(actorId, actorRole, actorStatus))
			return Result.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can rename cases."));

		if (name is null)
			return Result.Failure(new Error("CaseNameRequired", "OrderCommands name is required."));

		if (Status == OrderStatus.Closed)
			return Result.Failure(new Error("CaseClosed", "A closed case cannot be renamed."));

		Name = name;
		return Result.Success();
	}

	public Result Close(UserId actorId, UserRole actorRole, UserStatus actorStatus)
	{
		if (!CanManage(actorId, actorRole, actorStatus))
			return Result.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can close cases."));

		if (Status == OrderStatus.Closed)
			return Result.Failure(new Error("CaseAlreadyClosed", "OrderCommands is already closed."));

		Status = OrderStatus.Closed;
		ClosedAt = DateTime.UtcNow;
		return Result.Success();
	}

	public Result Reopen(UserId actorId, UserRole actorRole, UserStatus actorStatus)
	{
		if (!CanManage(actorId, actorRole, actorStatus))
			return Result.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can reopen cases."));

		if (Status == OrderStatus.Open)
			return Result.Failure(new Error("CaseAlreadyOpen", "OrderCommands is already open."));

		Status = OrderStatus.Open;
		ClosedAt = null;
		return Result.Success();
	}

	internal Result AddWorkItem(Task workItem)
	{
		if (workItem is null)
			return Result.Failure(new Error("WorkItemRequired", "A work item is required."));

		if (Status == OrderStatus.Closed)
			return Result.Failure(new Error("CaseClosed", "A work item cannot be added to a closed case."));

		if (_tasks.Any(item => item.TaskId == workItem.TaskId))
			return Result.Failure(new Error("WorkItemAlreadyAdded", "The work item already belongs to this case."));

		var attachResult = workItem.AttachTo(Id);
		if (attachResult.IsFailure)
			return attachResult;

		_tasks.Add(workItem);
		return Result.Success();
	}

	internal Result RemoveWorkItem(TaskId taskId)
	{
		if (taskId is null)
			return Result.Failure(new Error("WorkItemRequired", "A work item is required."));

		var workItem = _tasks.FirstOrDefault(item => item.TaskId == taskId);
		if (workItem is null)
			return Result.Failure(new Error("WorkItemNotFound", "The work item does not belong to this case."));

		if (Status == OrderStatus.Closed)
			return Result.Failure(new Error("CaseClosed", "A work item cannot be removed from a closed case."));

		_tasks.Remove(workItem);
		workItem.Detach();
		return Result.Success();
	}

	public bool ContainsWorkItem(TaskId taskId) =>
		taskId is not null && _tasks.Any(item => item.TaskId == taskId);

	internal Result CanRegisterTime(
		TaskId taskId,
		UserId employeeId,
		UserId? employeeManagerId)
	{
		if (Status == OrderStatus.Closed)
			return Result.Failure(new Error("CaseClosed", "Entries cannot be added to a closed case."));

		var workItem = _tasks.FirstOrDefault(item => item.TaskId == taskId);
		if (workItem is null)
			return Result.Failure(new Error("WorkItemNotFound", "Work item does not belong to the selected case."));


		if (employeeId is null || employeeManagerId != ManagerId)
			return Result.Failure(new Error("ManagerScopeForbidden", "The employee cannot access this case."));

		return Result.Success();
	}

	private bool CanManage(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus) =>
		actorId is not null &&
		actorStatus == UserStatus.Active &&
		(actorRole is UserRole.Manager or UserRole.Admin) &&
		(actorRole == UserRole.Admin || actorId == ManagerId);

	public Result<Task> AddWorkItem(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus,
		string title,
		string description)
	{
		if (!CanManage(actorId, actorRole, actorStatus))
			return Result<Task>.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can create work items."));

		var workItemResult = Task.Create(title, description);
		if (workItemResult.IsFailure)
			return workItemResult;

		var addResult = AddWorkItem(workItemResult.Value);
		return addResult.IsFailure
			? Result<Task>.Failure(addResult.Errors.ToArray())
			: workItemResult;
	}

	public Result RemoveWorkItem(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus,
		TaskId taskId)
	{
		if (!CanManage(actorId, actorRole, actorStatus))
			return Result.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can remove work items."));

		return RemoveWorkItem(taskId);
	}
}