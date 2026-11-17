namespace Application.Commands;

public sealed record CreateDepartmentCommand(
    Guid ActorId,
    string Name);

public sealed record RenameDepartmentCommand(
    Guid ActorId,
    Guid DepartmentId,
    string Name);

/// <summary>Adds a user to a department, or changes whether the member manages it.</summary>
public sealed record SetDepartmentMemberCommand(
    Guid ActorId,
    Guid DepartmentId,
    Guid UserId,
    bool IsManager);

public sealed record RemoveDepartmentMemberCommand(
    Guid ActorId,
    Guid DepartmentId,
    Guid UserId);
