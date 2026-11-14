using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

/*  Summary
 *  Employees (previously Users) is the users of the time registration.
 */
public sealed class Employee
{
    private readonly List<EmployeeSystemAccess> _accesses = new();
    
    public EmployeeId Id { get; private set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    
    // Status for employee, if its active or deactivated.
    public EmployeeStatus Status { get; set; }
    
    // Nothing functional, just an optional title that can be put in to see the role of the employee if needed
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyCollection<EmployeeSystemAccess> Accesses => _accesses.AsReadOnly();

    public static Result<Employee> Create(string name, string email, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Employee>.Failure(new Error("EmployeeNameRequired", "An employee name is required."));
        if (string.IsNullOrWhiteSpace(email))
            return Result<Employee>.Failure(new Error("EmployeeEmailRequired", "An employee email is required."));
        if (createdAtUtc.Kind != DateTimeKind.Utc)
            return Result<Employee>.Failure(new Error("CreatedAtUtcRequired", "The creation time must be UTC."));

        var employee = new Employee
        {
            Id = EmployeeId.New(),
            Name = name.Trim(),
            Email = email.Trim(),
            Status = EmployeeStatus.Active,
            CreatedAt = createdAtUtc
        };
        employee._accesses.Add(new EmployeeSystemAccess
        {
            EmployeeId = employee.Id,
            Permission = SystemPermission.Employee
        });
        return Result<Employee>.Success(employee);
    }

    public bool HasPermission(SystemPermission permission) =>
        Status == EmployeeStatus.Active &&
        _accesses.Any(access => access.Permission == permission);

    public Result GrantPermission(SystemPermission permission)
    {
        if (Id is null)
            return Result.Failure(new Error("EmployeeIdRequired", "An employee ID is required."));
        if (!Enum.IsDefined(permission))
            return Result.Failure(new Error("InvalidSystemPermission", "The permission is invalid."));
        if (Status != EmployeeStatus.Active)
            return Result.Failure(new Error("EmployeeInactive", "An inactive employee cannot receive permissions."));
        if (_accesses.All(access => access.Permission != permission))
            _accesses.Add(new EmployeeSystemAccess { EmployeeId = Id, Permission = permission });
        return Result.Success();
    }

    public bool CanReviewTimeEntries() =>
        HasPermission(SystemPermission.Employee) && HasPermission(SystemPermission.CatagoryReviewer);
}

// Create, Edit,
