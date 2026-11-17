namespace WebAPI.Contracts.Accounts;

/// <summary>Permissions are system-access names such as "DepartmentManager"; Employee access is always granted.</summary>
public sealed record CreateEmployeeRequest(
    string Name,
    string Email,
    string Password,
    string? Title = null,
    IReadOnlyCollection<string>? Permissions = null);

public sealed record UpdateEmployeeRequest(string Name, string? Title = null);

public sealed record SetEmployeePermissionsRequest(IReadOnlyCollection<string> Permissions);

public sealed record CreateDepartmentRequest(string Name);

public sealed record CreatedResponse(Guid Id);

public sealed record ItemsResponse<T>(IReadOnlyList<T> Items);
