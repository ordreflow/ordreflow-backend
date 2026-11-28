namespace Application.Commands;

/// <summary>Creates an active employee with a local login. Every employee gets the baseline Employee access.</summary>
public sealed record CreateEmployeeCommand(
    Guid ActorId,
    string Name,
    string Email,
    string Password,
    string? Title,
    IReadOnlyCollection<string>? Permissions);

/// <summary>Changes the displayed name and title. The title never changes access (FR-13).</summary>
public sealed record UpdateEmployeeProfileCommand(Guid ActorId, Guid EmployeeId, string Name, string? Title);

/// <summary>Replaces the employee's system access with exactly the given grants (FR-13).</summary>
public sealed record SetEmployeePermissionsCommand(Guid ActorId, Guid EmployeeId, IReadOnlyCollection<string> Permissions);

/// <summary>Sets a new login password for the employee. Only administrators may do this.</summary>
public sealed record SetEmployeePasswordCommand(Guid ActorId, Guid EmployeeId, string Password);

public sealed record DeactivateEmployeeCommand(Guid ActorId, Guid EmployeeId);

public sealed record ActivateEmployeeCommand(Guid ActorId, Guid EmployeeId);

public sealed record CreateDepartmentCommand(Guid ActorId, string Name);

public sealed record AddDepartmentEmployeeCommand(Guid ActorId, Guid DepartmentId, Guid EmployeeId);

public sealed record RemoveDepartmentEmployeeCommand(Guid ActorId, Guid DepartmentId, Guid EmployeeId);

public sealed record AppointDepartmentManagerCommand(Guid ActorId, Guid DepartmentId, Guid EmployeeId);

public sealed record RemoveDepartmentManagerCommand(Guid ActorId, Guid DepartmentId, Guid EmployeeId);
