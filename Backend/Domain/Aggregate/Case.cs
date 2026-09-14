using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class Case
{
	private readonly List<WorkCase> _workItems = new();

	public CaseId Id { get; private set; } = null!;
	public CaseName Name { get; private set; } = null!;
	public CaseStatus Status { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime? ClosedAt { get; private set; }
	public IReadOnlyCollection<WorkCase> WorkItems => _workItems.AsReadOnly();

	private Case()
	{
	}

	private Case(CaseName name)
	{
		Id = CaseId.Create(Guid.NewGuid()).Value;
		Name = name;
		Status = CaseStatus.Open;
		CreatedAt = DateTime.UtcNow;
	}

	public static Result<Case> Create(
		UserRole actorRole,
		UserStatus actorStatus,
		CaseName name)
	{
		if (actorStatus != UserStatus.Active ||
			(actorRole is not UserRole.Manager and not UserRole.Admin))
			return Result<Case>.Failure(new Error("CaseCreationForbidden", "Only an active manager or admin can create cases."));

		if (name is null)
			return Result<Case>.Failure(new Error("CaseNameRequired", "Case name is required."));

		return Result<Case>.Success(new Case(name));
	}

	public Result Rename(
		UserRole actorRole,
		UserStatus actorStatus,
		CaseName name)
	{
		if (!CanManage(actorRole, actorStatus))
			return Result.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can rename cases."));

		if (name is null)
			return Result.Failure(new Error("CaseNameRequired", "Case name is required."));

		if (Status == CaseStatus.Closed)
			return Result.Failure(new Error("CaseClosed", "A closed case cannot be renamed."));

		Name = name;
		return Result.Success();
	}

	public Result Close(UserRole actorRole, UserStatus actorStatus)
	{
		if (!CanManage(actorRole, actorStatus))
			return Result.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can close cases."));

		if (Status == CaseStatus.Closed)
			return Result.Failure(new Error("CaseAlreadyClosed", "Case is already closed."));

		Status = CaseStatus.Closed;
		ClosedAt = DateTime.UtcNow;
		return Result.Success();
	}

	public Result Reopen(UserRole actorRole, UserStatus actorStatus)
	{
		if (!CanManage(actorRole, actorStatus))
			return Result.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can reopen cases."));

		if (Status == CaseStatus.Open)
			return Result.Failure(new Error("CaseAlreadyOpen", "Case is already open."));

		Status = CaseStatus.Open;
		ClosedAt = null;
		return Result.Success();
	}

	internal Result AddWorkItem(WorkCase workItem)
	{
		if (workItem is null)
			return Result.Failure(new Error("WorkItemRequired", "A work item is required."));

		if (Status == CaseStatus.Closed)
			return Result.Failure(new Error("CaseClosed", "A work item cannot be added to a closed case."));

		if (_workItems.Any(item => item.Id == workItem.Id))
			return Result.Failure(new Error("WorkItemAlreadyAdded", "The work item already belongs to this case."));

		var attachResult = workItem.AttachTo(Id);
		if (attachResult.IsFailure)
			return attachResult;

		_workItems.Add(workItem);
		return Result.Success();
	}

	internal Result RemoveWorkItem(WorkId workId)
	{
		if (workId is null)
			return Result.Failure(new Error("WorkItemRequired", "A work item is required."));

		var workItem = _workItems.FirstOrDefault(item => item.Id == workId);
		if (workItem is null)
			return Result.Failure(new Error("WorkItemNotFound", "The work item does not belong to this case."));

		if (Status == CaseStatus.Closed)
			return Result.Failure(new Error("CaseClosed", "A work item cannot be removed from a closed case."));

		_workItems.Remove(workItem);
		workItem.Detach();
		return Result.Success();
	}

	public bool ContainsWorkItem(WorkId workId) =>
		workId is not null && _workItems.Any(item => item.Id == workId);

	internal Result CanRegisterTime(WorkId workId)
	{
		if (Status == CaseStatus.Closed)
			return Result.Failure(new Error("CaseClosed", "Entries cannot be added to a closed case."));

		if (!ContainsWorkItem(workId))
			return Result.Failure(new Error("WorkItemNotFound", "Work item does not belong to the selected case."));

		return Result.Success();
	}

	private bool CanManage(
		UserRole actorRole,
		UserStatus actorStatus) =>
		actorStatus == UserStatus.Active &&
		(actorRole is UserRole.Manager or UserRole.Admin);

	public Result<WorkCase> AddWorkItem(
		UserRole actorRole,
		UserStatus actorStatus,
		string title,
		string description)
	{
		if (!CanManage(actorRole, actorStatus))
			return Result<WorkCase>.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can create work items."));

		var workItemResult = WorkCase.Create(title, description);
		if (workItemResult.IsFailure)
			return workItemResult;

		var addResult = AddWorkItem(workItemResult.Value);
		return addResult.IsFailure
			? Result<WorkCase>.Failure(addResult.Errors.ToArray())
			: workItemResult;
	}

	public Result RemoveWorkItem(
		UserRole actorRole,
		UserStatus actorStatus,
		WorkId workId)
	{
		if (!CanManage(actorRole, actorStatus))
			return Result.Failure(new Error("CaseManagementForbidden", "Only an active manager or admin can remove work items."));

		return RemoveWorkItem(workId);
	}
}