using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Task
{
	public TaskId TaskId { get; private set; } = null!;
	public OrderId? OrderId { get; private set; }

	public string Title { get; private set; } = null!;

	public string Description { get; private set; } = null!;

	private Task()
	{
	}

	private Task(
		TaskId id,
		string title,
		string description)
	{
		TaskId = id;
		Title = title;
		Description = description;
	}

	internal Result AttachTo(OrderId orderId)
	{
		if (orderId is null)
			return Result.Failure(new Error("CaseRequired", "A work item must belong to a case."));

		if (OrderId is not null && OrderId != orderId)
			return Result.Failure(new Error("WorkItemAlreadyOwned", "The work item already belongs to another case."));

		OrderId = orderId;
		return Result.Success();
	}

	internal void Detach() => OrderId = null;

	public static Result<Task> Create(
		string title,
		string description)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			return Result<Task>.Failure(
				new Error("WorkTitleRequired", "Work item title is required."));
		}

		title = title.Trim();

		if (title.Length > 200)
		{
			return Result<Task>.Failure(
				new Error("WorkTitleTooLong", "Work item title cannot exceed 200 characters."));
		}

		if (description is null)
		{
			return Result<Task>.Failure(
				new Error("WorkDescriptionRequired", "Work item description is required."));
		}

		description = description.Trim();

		if (description.Length > 2000)
		{
			return Result<Task>.Failure(
				new Error("WorkDescriptionTooLong", "Work item description cannot exceed 2000 characters."));
		}

		return Result<Task>.Success(
			new Task(
				TaskId.Create(Guid.NewGuid()).Value,
				title,
				description));
	}

	public Result ChangeTitle(string title)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			return Result.Failure(
				new Error("WorkTitleRequired", "Work item title is required."));
		}

		title = title.Trim();

		if (title.Length > 200)
		{
			return Result.Failure(
				new Error("WorkTitleTooLong", "Work item title cannot exceed 200 characters."));
		}

		Title = title;
		return Result.Success();
	}

	public Result ChangeDescription(string description)
	{
		if (description is null)
		{
			return Result.Failure(
				new Error("WorkDescriptionRequired", "Work item description is required."));
		}

		description = description.Trim();

		if (description.Length > 2000)
		{
			return Result.Failure(
				new Error("WorkDescriptionTooLong", "Work item description cannot exceed 2000 characters."));
		}

		Description = description;
		return Result.Success();
	}
}
