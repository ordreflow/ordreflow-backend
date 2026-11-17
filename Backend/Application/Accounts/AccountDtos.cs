using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Accounts;

public sealed record EmployeeDto(
    Guid Id,
    string Name,
    string Email,
    string? Title,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<string> Permissions);

public sealed record DepartmentPersonDto(
    Guid EmployeeId,
    string Name,
    string Email,
    string? Title,
    string Status);

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    bool ManagedByMe,
    IReadOnlyList<DepartmentPersonDto> Managers,
    IReadOnlyList<DepartmentPersonDto> Members);

internal static class AccountProjection
{
    public static EmployeeDto Employee(Employee employee) => new(
        employee.Id.Value,
        employee.Name.Value,
        employee.Email.Value,
        employee.Title,
        employee.Status.ToString(),
        employee.CreatedAt,
        employee.Accesses
            .Select(access => access.Permission)
            .Distinct()
            .OrderBy(permission => permission)
            .Select(permission => permission.ToString())
            .ToArray());

    public static DepartmentPersonDto Person(Employee employee) => new(
        employee.Id.Value,
        employee.Name.Value,
        employee.Email.Value,
        employee.Title,
        employee.Status.ToString());
}

internal static class AccountInput
{
    public const int MaxTitleLength = 100;

    /// <summary>A displayed title only; blank clears it. It never grants access (FR-13).</summary>
    public static Result<string?> Title(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<string?>.Success(null);

        value = value.Trim();
        return value.Length > MaxTitleLength
            ? Result<string?>.Failure(new Error("TitleTooLong", $"The title cannot exceed {MaxTitleLength} characters."))
            : Result<string?>.Success(value);
    }

    public static Result<IReadOnlyList<SystemPermission>> Permissions(IEnumerable<string>? values)
    {
        var permissions = new List<SystemPermission>();
        foreach (var value in values ?? [])
        {
            if (!Enum.TryParse<SystemPermission>(value, true, out var permission) || !Enum.IsDefined(permission) ||
                int.TryParse(value, out _))
                return Result<IReadOnlyList<SystemPermission>>.Failure(
                    new Error("InvalidSystemPermission", $"'{value}' is not a valid system access."));

            if (!permissions.Contains(permission))
                permissions.Add(permission);
        }

        return Result<IReadOnlyList<SystemPermission>>.Success(permissions);
    }

    public static Result<(EmployeeId Employee, DepartmentId Department)> Link(Guid departmentId, Guid employeeId)
    {
        var department = DepartmentId.Create(departmentId);
        var employee = EmployeeId.Create(employeeId);
        if (department.IsFailure || employee.IsFailure)
            return Result<(EmployeeId, DepartmentId)>.Failure(
                (department.IsFailure ? department.Errors : employee.Errors).ToArray());

        return Result<(EmployeeId, DepartmentId)>.Success((employee.Value, department.Value));
    }

    public static readonly Error EmployeeNotFound = new("EmployeeNotFound", "The employee could not be found.");
    public static readonly Error DepartmentNotFound = new("DepartmentNotFound", "The department could not be found.");
}
