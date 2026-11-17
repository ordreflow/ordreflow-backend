namespace Domain.ValueObjects;

/// <summary>What kind of work a task is (FR-08). It is descriptive and does not change access.</summary>
public enum WorkItemKind
{
    Task = 1,
    Project = 2,
    Ticket = 3
}
