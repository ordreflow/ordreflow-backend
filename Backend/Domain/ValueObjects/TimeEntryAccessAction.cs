namespace Domain.ValueObjects;

[Flags]
public enum TimeEntryAccessAction
{
    None = 0,
    View = 1,
    Create = 2,
    Correct = 4,
    Review = 8,
    Export = 16
}
