namespace Domain.ValueObjects;

public enum TimeEntryStatus
{
    Draft = 1,
    ReadyForApproval = 2,
    Approved = 3,
    Rejected = 4,
    Closed = 5,
    Deleted = 6
}
