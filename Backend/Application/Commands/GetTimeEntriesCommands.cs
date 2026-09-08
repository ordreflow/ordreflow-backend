namespace Application.Commands;

public class GetTimeEntriesCommand
{

    public DateTime StartDate { get; }

    public DateTime EndDate { get; }

    public GetTimeEntriesCommand(
        DateTime startDate,
        DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}