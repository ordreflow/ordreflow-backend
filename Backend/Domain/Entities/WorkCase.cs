using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class WorkCase
{
	public WorkId Id { get; private set; } = null!;
	public CaseId? CaseId { get; private set; }

	public string Title { get; private set; } = null!;

	public string Description { get; private set; } = null!;

	private WorkCase()
	{
	}

	private WorkCase(
		WorkId id,
		string title,
		string description)
	{
		Id = id;
		Title = title;
		Description = description;
	}

	internal Result AttachTo(CaseId caseId)
	{
		if (caseId is null)
			return Result.Failure(new Error("CaseRequired", "A work item must belong to a case."));

		if (CaseId is not null && CaseId != caseId)
			return Result.Failure(new Error("WorkItemAlreadyOwned", "The work item already belongs to another case."));

		CaseId = caseId;
		return Result.Success();
	}

	internal void Detach() => CaseId = null;

	public static Result<WorkCase> Create(
		string title,
		string description)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			return Result<WorkCase>.Failure(
				new Error("WorkTitleRequired", "Work item title is required."));
		}

		title = title.Trim();

		if (title.Length > 200)
		{
			return Result<WorkCase>.Failure(
				new Error("WorkTitleTooLong", "Work item title cannot exceed 200 characters."));
		}

		if (description is null)
		{
			return Result<WorkCase>.Failure(
				new Error("WorkDescriptionRequired", "Work item description is required."));
		}

		description = description.Trim();

		if (description.Length > 2000)
		{
			return Result<WorkCase>.Failure(
				new Error("WorkDescriptionTooLong", "Work item description cannot exceed 2000 characters."));
		}

		return Result<WorkCase>.Success(
			new WorkCase(
				WorkId.Create(Guid.NewGuid()).Value,
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
