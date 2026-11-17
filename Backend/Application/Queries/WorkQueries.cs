namespace Application.Queries;

/// <summary>The categories, with their work, that the actor may plan work in (FR-08).</summary>
public sealed record ListPlannableCategoriesQuery(Guid ActorId);
