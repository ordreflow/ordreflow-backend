namespace Application.Queries;

/// <summary>Admins see every department. Other users see the departments they belong to.</summary>
public sealed record GetDepartmentsQuery(Guid ActorId);

/// <summary>The active employees in the departments the actor manages, i.e. who the actor may plan work for.</summary>
public sealed record GetManagedEmployeesQuery(Guid ActorId);
