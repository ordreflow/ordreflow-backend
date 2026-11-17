namespace Application.Queries;

/// <summary>
/// The work the employee may select (FR-09). With <paramref name="AssignedToMe"/> the same authorised list is narrowed
/// to work assigned to the employee (FR-32); turning it off shows general work again (USE-04).
/// </summary>
public sealed record ListAccessibleWorkQuery(Guid ActorId, bool AssignedToMe = false, string? Search = null);

/// <summary>One work item by direct URL; hidden work is reported as not found (FR-21).</summary>
public sealed record GetAccessibleWorkQuery(Guid ActorId, Guid WorkItemId);
