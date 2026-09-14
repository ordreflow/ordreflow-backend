using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services;

public sealed class TimeRegistrationDomainService
{
	public Result Register(
		TimeSheet timeSheet,
		UserId employeeId,
		UserStatus employeeStatus,
		Case workCase,
		TimeEntry entry)
	{
		if (timeSheet is null)
			return Result.Failure(new Error("TimeSheetRequired", "A timesheet is required."));

		if (employeeId is null || employeeId != timeSheet.UserId)
			return Result.Failure(new Error("EntryOwnerMismatch", "Only the timesheet owner can add entries."));

		if (employeeStatus != UserStatus.Active)
			return Result.Failure(new Error("UserInactive", "An inactive user cannot add entries."));

		if (workCase is null)
			return Result.Failure(new Error("CaseRequired", "A case is required."));

		if (entry is null)
			return Result.Failure(new Error("TimeEntryRequired", "Time entry is required."));

		var caseValidation = workCase.CanRegisterTime(entry.WorkId);
		if (caseValidation.IsFailure)
			return caseValidation;

		return timeSheet.AddEntry(employeeId, entry);
	}
}