namespace Domain.Entities;


public class TimeEntry
{
    public int Id { get; private set; }

    public int WorkItemId { get; private set; }

    public DateTime Date { get; private set; }

    public decimal Hours { get; private set; }

    public TimeSpan? StartTime { get; private set; }

    public TimeSpan? EndTime { get; private set; }

    public string? Comment { get; private set; }

    private TimeEntry()
    {
    }

    public TimeEntry(
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