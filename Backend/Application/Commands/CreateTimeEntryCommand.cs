namespace Application.Commands;

public class CreateTimeEntryCommand
{
    public int WorkItemId { get; }

    public DateTime Date { get; }

    public decimal Hours { get; }

    public TimeSpan? StartTime { get; }

    public TimeSpan? EndTime { get; }

    public string? Comment { get; }
    public int Id { get; set; }

    public CreateTimeEntryCommand(
        int workItemId,
        DateTime date,
        decimal hours,
        TimeSpan? startTime,
        TimeSpan? endTime,
        string? comment)
    {
        WorkItemId = workItemId;
        Date = date;
        Hours = hours;
        StartTime = startTime;
        EndTime = endTime;
        Comment = comment;
    }
}