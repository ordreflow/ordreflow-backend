using Domain.Aggregate;
using Domain.Entities;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTest.Features;

public class TimeRegistrationProcessTests
{
	[Fact]
	public void CompleteTimeRegistrationProcess_ManagerCreatesWork_EmployeeRegisters_ManagerApproves()
	{
		Console.WriteLine("=== Time registration process ===");

		var companyName = CompanyName.Create("Elektronik A/S");
		var subdomain = Subdomain.Create("elektronik");
		var tenant = Tenant.Create(companyName.Value, subdomain.Value).Value;
		Console.WriteLine($"1. Tenant created: {tenant.Name.Value}");

		var managerName = PersonName.Create("Manager Mads").Value;
		var managerEmail = EmailAddress.Create("manager@elektronik.dk").Value;
		var manager = User.Create(
			tenant.Id,
			managerName,
			managerEmail,
			UserRole.Manager).Value;
		Console.WriteLine($"2. Manager created: {manager.Name.Value}");

		var employeeName = PersonName.Create("Peter Hansen").Value;
		var employeeEmail = EmailAddress.Create("peter@elektronik.dk").Value;
		var employeeResult = manager.CreateUser(
			employeeName,
			employeeEmail,
			UserRole.Employee);

		Assert.True(employeeResult.IsSuccess);
		var employee = employeeResult.Value;
		Console.WriteLine($"3. Employee created by manager: {employee.Name.Value}");

		var caseResult = Case.Create(
			manager.TenantId,
			manager.Role,
			manager.Status,
			CaseName.Create("Reparation af styringsenhed").Value);

		Assert.True(caseResult.IsSuccess);
		var workCaseResult = caseResult.Value.AddWorkItem(
			manager.TenantId,
			manager.Role,
			manager.Status,
			"Udskift printkort",
			"Udskift og test defekt printkort.");

		Assert.True(workCaseResult.IsSuccess);
		var workCase = workCaseResult.Value;
		Console.WriteLine($"4. Manager created case and work item: {workCase.Title}");

		var timeSheet = TimeSheet.Create(
			tenant.Id,
			employee.Id,
			2026,
			9).Value;
		Console.WriteLine("5. Employee has a September timesheet");

		var timeEntry = TimeEntry.Create(
			workCase.Id,
			new DateTime(2026, 9, 9),
			7.5m,
			new TimeSpan(8, 0, 0),
			new TimeSpan(15, 30, 0),
			"Udskiftede og testede printkortet").Value;

		var registrationService = new TimeRegistrationDomainService();
		var addEntryResult = registrationService.Register(
			timeSheet,
			employee.TenantId,
			employee.Id,
			employee.Status,
			caseResult.Value,
			timeEntry);

		Assert.True(addEntryResult.IsSuccess);
		Assert.Equal(7.5m, timeSheet.TotalHours);
		Assert.Equal(TimeEntryStatus.Draft, timeEntry.Status);
		Console.WriteLine($"6. Employee registered {timeEntry.Hours} hours");

		var approveEntryResult = timeSheet.ApproveEntry(
			manager.TenantId,
			manager.Id,
			manager.Role,
			manager.Status,
			timeEntry);

		Assert.True(approveEntryResult.IsSuccess);
		Assert.Equal(TimeEntryStatus.Approved, timeEntry.Status);
		Console.WriteLine("7. Manager approved time entry");

		var lockEntryResult = timeSheet.LockEntry(
			manager.TenantId,
			manager.Id,
			manager.Role,
			manager.Status,
			timeEntry);

		Assert.True(lockEntryResult.IsSuccess);
		Assert.Equal(TimeEntryStatus.Locked, timeEntry.Status);
		Console.WriteLine("8. Manager locked time entry");
		Console.WriteLine($"Total approved hours: {timeSheet.TotalHours}");
		Console.WriteLine("=== Process completed ===");
	}
}
