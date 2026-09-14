using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class TimeSheet
{
	private readonly List<TimeEntry> _entries = new();

	public TimeSheetId Id { get; private set; } = null!;
	public UserId UserId { get; private set; } = null!;
	public int Year { get; private set; }
	public int Month { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();
	public decimal TotalHours => _entries.Sum(entry => entry.Hours);

	private TimeSheet()
	{
	}

	private TimeSheet(UserId userId, int year, int month)
	{
		Id = TimeSheetId.Create(Guid.NewGuid()).Value;
		UserId = userId;
		Year = year;
		Month = month;
		CreatedAt = DateTime.UtcNow;
	}

	public static Result<TimeSheet> Create(UserId userId, int year, int month)
	{
		if (userId is null)
			return Result<TimeSheet>.Failure(new Error("UserRequired", "A timesheet must belong to a user."));

		if (year < 1 || year > 9999)
			return Result<TimeSheet>.Failure(new Error("InvalidYear", "Timesheet year must be between 1 and 9999."));

		if (month < 1 || month > 12)
			return Result<TimeSheet>.Failure(new Error("InvalidMonth", "Timesheet month must be between 1 and 12."));

		return Result<TimeSheet>.Success(new TimeSheet(userId, year, month));
	}

	internal Result AddEntry(UserId employeeId, TimeEntry entry)
	{
		if (employeeId is null || employeeId != UserId)
			return Result.Failure(new Error("EntryOwnerMismatch", "Only the timesheet owner can add entries."));

		if (entry is null)
			return Result.Failure(new Error("TimeEntryRequired", "Time entry is required."));

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

	public decimal GetDayTotal(DateOnly date) =>
		_entries
			.Where(entry => DateOnly.FromDateTime(entry.Date) == date)
			.Sum(entry => entry.Hours);

	public IReadOnlyCollection<TimeEntry> GetEntriesForDay(DateOnly date) =>
		_entries
			.Where(entry => DateOnly.FromDateTime(entry.Date) == date)
			.ToArray();

	public IReadOnlyCollection<TimeEntry> GetEntriesForWeek(DateOnly weekStart) =>
		_entries
			.Where(entry =>
				DateOnly.FromDateTime(entry.Date) >= weekStart &&
				DateOnly.FromDateTime(entry.Date) < weekStart.AddDays(7))
			.ToArray();

	public Result ChangeEntryHours(UserId employeeId, TimeEntry entry, decimal hours)
	{
		var accessResult = CanEdit(employeeId, entry);
		if (accessResult.IsFailure)
			return accessResult;

		var dailyHours = _entries
			.Where(existing => existing != entry && existing.Date.Date == entry.Date.Date)
			.Sum(existing => existing.Hours);
		if (dailyHours + hours > 24)
			return Result.Failure(new Error("DailyHoursExceeded", "Total hours for a day cannot exceed 24."));

		return entry.ChangeHours(hours);
	}

	public Result ApproveEntry(
		UserId approverId,
		UserRole approverRole,
		UserStatus approverStatus,
		TimeEntry entry)
	{
		if (!CanManageEntry(approverId, approverRole, approverStatus, entry))
			return Result.Failure(new Error("ApprovalForbidden", "Only an active manager or admin can approve this time entry."));

		return entry.Approve();
	}

	public Result RejectEntry(
		UserId approverId,
		UserRole approverRole,
		UserStatus approverStatus,
		TimeEntry entry)
	{
		if (!CanManageEntry(approverId, approverRole, approverStatus, entry))
			return Result.Failure(new Error("ApprovalForbidden", "Only an active manager or admin can reject this time entry."));

		return entry.Reject();
	}

	public Result ReopenEntry(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus,
		TimeEntry entry)
	{
		if (entry is null || !_entries.Contains(entry))
			return Result.Failure(new Error("EntryAccessForbidden", "The time entry does not belong to this timesheet."));

		var isManager = actorStatus == UserStatus.Active &&
			(actorRole is UserRole.Manager or UserRole.Admin);
		if (actorId != UserId && !isManager)
			return Result.Failure(new Error("EntryAccessForbidden", "Only the owner or a manager can reopen this time entry."));

		return entry.Reopen();
	}

	public Result LockEntry(
		UserId approverId,
		UserRole approverRole,
		UserStatus approverStatus,
		TimeEntry entry)
	{
		if (!CanManageEntry(approverId, approverRole, approverStatus, entry))
			return Result.Failure(new Error("ApprovalForbidden", "Only an active manager or admin can lock this time entry."));

		return entry.Lock();
	}

	public Result ChangeEntryDate(UserId employeeId, TimeEntry entry, DateTime date)
	{
		var accessResult = CanEdit(employeeId, entry);
		if (accessResult.IsFailure)
			return accessResult;

		if (date.Year != Year || date.Month != Month)
			return Result.Failure(new Error("EntryOutsidePeriod", "Entry date must belong to the timesheet month."));

		var dailyHours = _entries
			.Where(existing => existing != entry && existing.Date.Date == date.Date)
			.Sum(existing => existing.Hours);
		if (dailyHours + entry.Hours > 24)
			return Result.Failure(new Error("DailyHoursExceeded", "Total hours for a day cannot exceed 24."));

		return entry.ChangeDate(date);
	}

	public Result ChangeEntryComment(UserId employeeId, TimeEntry entry, string? comment)
	{
		var accessResult = CanEdit(employeeId, entry);
		if (accessResult.IsFailure)
			return accessResult;

		return entry.ChangeComment(comment);
	}

	public Result ChangeEntryTime(UserId employeeId, TimeEntry entry, TimeSpan? startTime, TimeSpan? endTime)
	{
		var accessResult = CanEdit(employeeId, entry);
		if (accessResult.IsFailure)
			return accessResult;

		return entry.ChangeTime(startTime, endTime);
	}

	private Result CanEdit(UserId employeeId, TimeEntry entry)
	{
		if (employeeId is null || employeeId != UserId)
			return Result.Failure(new Error("EntryOwnerMismatch", "Only the timesheet owner can edit this time entry."));

		if (entry is null || !_entries.Contains(entry))
			return Result.Failure(new Error("EntryNotFound", "The time entry does not belong to this timesheet."));

		if (entry.Status is not (TimeEntryStatus.Draft or TimeEntryStatus.Rejected))
			return Result.Failure(new Error("TimeEntryNotEditable", "Only draft or rejected time entries can be edited."));

		return Result.Success();
	}

	private bool CanManageEntry(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus,
		TimeEntry entry) =>
		actorId is not null && actorStatus == UserStatus.Active &&
		(actorRole is UserRole.Manager or UserRole.Admin) &&
		entry is not null && _entries.Contains(entry);

}