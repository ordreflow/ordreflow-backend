namespace Application.Queries;

public sealed record GetMyProfileQuery(Guid ActorId);

public sealed record ListEmployeesQuery(Guid ActorId);

public sealed record GetEmployeeQuery(Guid ActorId, Guid EmployeeId);

/// <summary>Administrators see every department; others see the departments they belong to or manage.</summary>
public sealed record ListDepartmentsQuery(Guid ActorId);

/// <summary>The active employees in the departments the actor manages: who the actor may plan work for (FR-14).</summary>
public sealed record ListManagedEmployeesQuery(Guid ActorId);
