using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Services;

// Coordinates business rules across Employee, Department, and TimeEntry.
// Loading data, resolving the company-local date, and saving belong to Application.
public sealed class TimeRegistrationDomainService
{
    public Result ValidateEmployee(Employee? employee) =>
        employee is not null && employee.HasPermission(SystemPermission.Employee)
            ? Result.Success()
            : Result.Failure(new Error("EmployeeAccessForbidden", "Active employee permission is required."));

    public Result<TimeEntry> Register(Employee employee, Department department, TaskId taskId,
        WorkDate workDate, Duration duration, TimeEntryDescription description,
        TimeRegistrationAccess access, WorkDatePeriod permittedPeriod, DateTime recordedAtUtc)
    {
        var employeeAccess = ValidateEmployee(employee);
        if (employeeAccess.IsFailure)
            return Result<TimeEntry>.Failure(employeeAccess.Errors.ToArray());
        if (department is null)
            return Result<TimeEntry>.Failure(new Error("DepartmentRequired", "The owning department is required."));
        var available = department.CanRegisterTime(taskId);
        if (available.IsFailure)
            return Result<TimeEntry>.Failure(available.Errors.ToArray());
        if (access is null)
            return Result<TimeEntry>.Failure(new Error("RegistrationAccessRequired", "Registration access facts are required."));

        var catagory = department.FindCatagoryForTask(taskId)!;
        if (catagory.InternalOnly && !access.IsDepartmentMember)
            return Result<TimeEntry>.Failure(new Error("CatagoryAccessForbidden", "This category is restricted to its department."));
        var task = catagory.Tasks.Single(item => item.Id == taskId);
        if ((access.HasAssignments || task.AssignmentOnly) && !access.IsAssigned)
            return Result<TimeEntry>.Failure(new Error("TaskAssignmentForbidden", "This task is assigned to other employees."));
        if (recordedAtUtc.Kind != DateTimeKind.Utc)
            return Result<TimeEntry>.Failure(new Error("AuditTimeUtcRequired", "The registration timestamp must be UTC."));

        return TimeEntry.Record(employee.Id, taskId, workDate, duration, description, permittedPeriod, recordedAtUtc);
    }
}
