namespace Domain.ValueObjects;

public enum TimeEntryAuditEventType
{
    Created = 1,
    Submitted = 2,
    Corrected = 3,
    Reviewed = 4,
    Finalized = 5
}
