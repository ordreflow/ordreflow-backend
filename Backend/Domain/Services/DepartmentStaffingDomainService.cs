using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
/// Rules for placing employees in departments (FR-14). Membership and management are links between the
/// Employee and Department aggregates. Managing a department requires the DepartmentManager system grant;
/// a displayed title such as "Manager" never grants it (FR-13, SEC-01).
/// </summary>
public sealed class DepartmentStaffingDomainService
{
    public Result<DepartmentEmployee> AddMember(Department department, Employee employee)
    {
        var valid = Validate(department, employee);
        if (valid.IsFailure)
            return Result<DepartmentEmployee>.Failure(valid.Errors.ToArray());

        return Result<DepartmentEmployee>.Success(new DepartmentEmployee
        {
            DepartmentId = department.Id,
            EmployeeId = employee.Id
        });
    }

    public Result<DepartmentManager> AppointManager(Department department, Employee employee)
    {
        var valid = Validate(department, employee);
        if (valid.IsFailure)
            return Result<DepartmentManager>.Failure(valid.Errors.ToArray());

        if (!employee.HasPermission(SystemPermission.DepartmentManager))
            return Result<DepartmentManager>.Failure(new Error("DepartmentManagerAccessRequired",
                "Only employees with department-manager access can manage a department."));

        return Result<DepartmentManager>.Success(new DepartmentManager
        {
            DepartmentId = department.Id,
            ManagerId = employee.Id
        });
    }

    /// <summary>Department scope only counts while the manager is active and still has the grant.</summary>
    public static bool HasManagerAccess(Employee employee) =>
        employee.HasPermission(SystemPermission.Employee) &&
        employee.HasPermission(SystemPermission.DepartmentManager);

    private static Result Validate(Department department, Employee employee)
    {
        if (department is null || employee is null)
            return Result.Failure(new Error("StaffingDetailsRequired", "A department and an employee are required."));

        return employee.Status == EmployeeStatus.Active
            ? Result.Success()
            : Result.Failure(new Error("EmployeeInactive", "Only active employees can be placed in a department."));
    }
}
