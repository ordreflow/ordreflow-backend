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
    public PersonName Name { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    
    // Status for employee, if its active or deactivated.
    public EmployeeStatus Status { get; private set; }
    
    // Nothing functional, just an optional title that can be put in to see the role of the employee if needed
    public string? Title { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<EmployeeSystemAccess> Accesses => _accesses.AsReadOnly();

    private Employee() { }

    public static Result<Employee> Create(string name, string email, DateTime createdAtUtc)
    {
        var personName = PersonName.Create(name);
        if (personName.IsFailure)
            return Result<Employee>.Failure(personName.Errors.ToArray());
        var emailAddress = EmailAddress.Create(email);
        if (emailAddress.IsFailure)
            return Result<Employee>.Failure(emailAddress.Errors.ToArray());
        if (createdAtUtc == default || createdAtUtc.Kind != DateTimeKind.Utc)
            return Result<Employee>.Failure(new Error("CreatedAtUtcRequired", "The creation time must be UTC."));

        var employee = new Employee
        {
            Id = EmployeeId.Create(Guid.NewGuid()).Value,
            Name = personName.Value,
            Email = emailAddress.Value,
            Status = EmployeeStatus.Active,
            CreatedAt = createdAtUtc
        };
        employee._accesses.Add(EmployeeSystemAccess.Create(employee.Id, SystemPermission.Employee));
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
            _accesses.Add(EmployeeSystemAccess.Create(Id, permission));
        return Result.Success();
    }

    public bool CanReviewTimeEntries() =>
        HasPermission(SystemPermission.Employee) && HasPermission(SystemPermission.CatagoryReviewer);

    public bool CanExportTimeEntries() => HasPermission(SystemPermission.Employee) &&
        (HasPermission(SystemPermission.Payroll) || HasPermission(SystemPermission.Administrator));

    public bool CanViewTimeReports() => HasPermission(SystemPermission.Employee) &&
        (HasPermission(SystemPermission.DepartmentManager) || HasPermission(SystemPermission.Administrator));

    public Result RevokePermission(SystemPermission permission)
    {
        if (!Enum.IsDefined(permission))
            return Result.Failure(new Error("InvalidSystemPermission", "The permission is invalid."));
        _accesses.RemoveAll(access => access.Permission == permission);
        return Result.Success();
    }

    public Result ChangeProfile(PersonName name, EmailAddress email, string? title = null)
    {
        if (name is null || email is null)
            return Result.Failure(new Error("EmployeeDetailsRequired", "A name and email address are required."));
        Name = name;
        Email = email;
        Title = title?.Trim();
        return Result.Success();
    }

    public void Deactivate() => Status = EmployeeStatus.Deactivated;
    public void Activate() => Status = EmployeeStatus.Active;
}

// Create, Edit,
