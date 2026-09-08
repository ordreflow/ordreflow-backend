using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class TimeSheet
{
	private readonly List<TimeEntry> _entries = new();

	public TimeSheetId Id { get; private set; } = null!;
	public TenantId TenantId { get; private set; } = null!;
	public UserId UserId { get; private set; } = null!;
	public int Year { get; private set; }
	public int Month { get; private set; }
	public TimeSheetStatus Status { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime? SubmittedAt { get; private set; }
	public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();
	public decimal TotalHours => _entries.Sum(entry => entry.Hours);

	private TimeSheet()
	{
	}

	private TimeSheet(TenantId tenantId, User user, int year, int month)
	{
		Id = TimeSheetId.Create(Guid.NewGuid()).Value;
		TenantId = tenantId;
		UserId = user.Id;
		Year = year;
		Month = month;
		Status = TimeSheetStatus.Draft;
		CreatedAt = DateTime.UtcNow;
	}

	public static Result<TimeSheet> Create(TenantId tenantId, User user, int year, int month)
	{
		if (tenantId is null)
			return Result<TimeSheet>.Failure(new Error("TenantRequired", "A timesheet must belong to a tenant."));

		if (user is null)
			return Result<TimeSheet>.Failure(new Error("UserRequired", "A timesheet must belong to a user."));

		if (user.TenantId != tenantId)
			return Result<TimeSheet>.Failure(new Error("TenantMismatch", "User and timesheet must belong to the same tenant."));

		if (user.Status != UserStatus.Active)
			return Result<TimeSheet>.Failure(new Error("UserInactive", "An inactive user cannot have a timesheet."));

		if (year < 1 || year > 9999)
			return Result<TimeSheet>.Failure(new Error("InvalidYear", "Timesheet year must be between 1 and 9999."));

		if (month < 1 || month > 12)
			return Result<TimeSheet>.Failure(new Error("InvalidMonth", "Timesheet month must be between 1 and 12."));

		return Result<TimeSheet>.Success(new TimeSheet(tenantId, user, year, month));
	}

	public Result AddEntry(TimeEntry entry, Case workCase)
	{
		if (Status != TimeSheetStatus.Draft)
			return Result.Failure(new Error("TimeSheetNotDraft", "Entries can only be added to a draft timesheet."));

		if (entry is null)
			return Result.Failure(new Error("TimeEntryRequired", "Time entry is required."));

		if (workCase is null || workCase.TenantId != TenantId)
			return Result.Failure(new Error("TenantMismatch", "Case and timesheet must belong to the same tenant."));

		if (!workCase.ContainsWorkItem(entry.WorkId))
			return Result.Failure(new Error("WorkItemNotFound", "Work item does not belong to the selected case."));

		var entryDate = entry.Date.Date;
		if (entryDate.Year != Year || entryDate.Month != Month)
			return Result.Failure(new Error("EntryOutsidePeriod", "Entry date must belong to the timesheet month."));

		var dailyHours = _entries
			.Where(existing => existing.Date.Date == entryDate)
			.Sum(existing => existing.Hours);
		if (dailyHours + entry.Hours > 24)
			return Result.Failure(new Error("DailyHoursExceeded", "Total hours for a day cannot exceed 24."));

		var assignmentResult = entry.AssignTo(Id);
		if (assignmentResult.IsFailure)
			return assignmentResult;

		_entries.Add(entry);
		return Result.Success();
	}

	public decimal GetWeekTotal(DateOnly weekStart)
	{
		var weekEnd = weekStart.AddDays(7);
		return _entries
			.Where(entry => DateOnly.FromDateTime(entry.Date) >= weekStart &&
				DateOnly.FromDateTime(entry.Date) < weekEnd)
			.Sum(entry => entry.Hours);
	}

	public Result ChangeEntryHours(TimeEntry entry, decimal hours)
	{
		if (!CanEdit(entry))
			return Result.Failure(new Error("TimeSheetNotEditable", "Only entries in a draft timesheet can be edited."));

		var dailyHours = _entries
			.Where(existing => existing != entry && existing.Date.Date == entry.Date.Date)
			.Sum(existing => existing.Hours);
		if (dailyHours + hours > 24)
			return Result.Failure(new Error("DailyHoursExceeded", "Total hours for a day cannot exceed 24."));

		return entry.ChangeHours(hours);
	}

	public Result ChangeEntryDate(TimeEntry entry, DateTime date)
	{
		if (!CanEdit(entry))
			return Result.Failure(new Error("TimeSheetNotEditable", "Only entries in a draft timesheet can be edited."));

		if (date.Year != Year || date.Month != Month)
			return Result.Failure(new Error("EntryOutsidePeriod", "Entry date must belong to the timesheet month."));

		var dailyHours = _entries
			.Where(existing => existing != entry && existing.Date.Date == date.Date)
			.Sum(existing => existing.Hours);
		if (dailyHours + entry.Hours > 24)
			return Result.Failure(new Error("DailyHoursExceeded", "Total hours for a day cannot exceed 24."));

		return entry.ChangeDate(date);
	}

	public Result ChangeEntryComment(TimeEntry entry, string? comment)
	{
		if (!CanEdit(entry))
			return Result.Failure(new Error("TimeSheetNotEditable", "Only entries in a draft timesheet can be edited."));

		return entry.ChangeComment(comment);
	}

	private bool CanEdit(TimeEntry entry) =>
		Status == TimeSheetStatus.Draft && entry is not null && _entries.Contains(entry);

	public Result Submit()
	{
		if (Status != TimeSheetStatus.Draft)
			return Result.Failure(new Error("TimeSheetNotDraft", "Only a draft timesheet can be submitted."));

		if (_entries.Count == 0)
			return Result.Failure(new Error("TimeSheetEmpty", "A timesheet must contain at least one entry."));

		Status = TimeSheetStatus.Submitted;
		SubmittedAt = DateTime.UtcNow;
		return Result.Success();
	}

	public Result Reopen(User actor)
	{
		if (actor is null || actor.TenantId != TenantId)
			return Result.Failure(new Error("TenantMismatch", "User and timesheet must belong to the same tenant."));

		var canReopen = Status == TimeSheetStatus.Rejected && actor.Id == UserId ||
			Status == TimeSheetStatus.Submitted && actor.CanManageUsers;
		if (!canReopen)
			return Result.Failure(new Error("TimeSheetNotReopenable", "Only a submitted or rejected timesheet can be reopened."));

		Status = TimeSheetStatus.Draft;
		SubmittedAt = null;
		return Result.Success();
	}

	public Result Approve(User approver)
	{
		if (!CanApprove(approver))
			return Result.Failure(new Error("ApprovalForbidden", "Only an active manager or admin from this tenant can approve."));

		if (Status != TimeSheetStatus.Submitted)
			return Result.Failure(new Error("TimeSheetNotSubmitted", "Only a submitted timesheet can be approved."));

		Status = TimeSheetStatus.Approved;
		return Result.Success();
	}

	public Result Reject(User approver)
	{
		if (!CanApprove(approver))
			return Result.Failure(new Error("ApprovalForbidden", "Only an active manager or admin from this tenant can reject."));

		if (Status != TimeSheetStatus.Submitted)
			return Result.Failure(new Error("TimeSheetNotSubmitted", "Only a submitted timesheet can be rejected."));

		Status = TimeSheetStatus.Rejected;
		return Result.Success();
	}

	public Result Lock(User approver)
	{
		if (!CanApprove(approver))
			return Result.Failure(new Error("LockForbidden", "Only an active manager or admin from this tenant can lock."));

		if (Status != TimeSheetStatus.Approved)
			return Result.Failure(new Error("TimeSheetNotApproved", "Only an approved timesheet can be locked."));

		Status = TimeSheetStatus.Locked;
		return Result.Success();
	}

	private bool CanApprove(User user) =>
		user is not null && user.TenantId == TenantId && user.CanManageUsers;
}