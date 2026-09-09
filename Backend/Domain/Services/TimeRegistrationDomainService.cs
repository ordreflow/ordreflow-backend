using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services;

public sealed class TimeRegistrationDomainService
{
	public Result Register(
		TimeSheet timeSheet,
		TenantId employeeTenantId,
		UserId employeeId,
		UserStatus employeeStatus,
		Case workCase,
		TimeEntry entry)
	{
		if (timeSheet is null)
			return Result.Failure(new Error("TimeSheetRequired", "A timesheet is required."));

		if (employeeTenantId is null || employeeTenantId != timeSheet.TenantId ||
			employeeId is null || employeeId != timeSheet.UserId)
			return Result.Failure(new Error("EntryOwnerMismatch", "Only the timesheet owner from the same tenant can add entries."));

		if (employeeStatus != UserStatus.Active)
			return Result.Failure(new Error("UserInactive", "An inactive user cannot add entries."));

		if (workCase is null || workCase.TenantId != timeSheet.TenantId)
			return Result.Failure(new Error("TenantMismatch", "Case and timesheet must belong to the same tenant."));

		if (entry is null)
			return Result.Failure(new Error("TimeEntryRequired", "Time entry is required."));

		var caseValidation = workCase.CanRegisterTime(entry.WorkId);
		if (caseValidation.IsFailure)
			return caseValidation;

		return timeSheet.AddEntry(employeeId, entry);
	}
}