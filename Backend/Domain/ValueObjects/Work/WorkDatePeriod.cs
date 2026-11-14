using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record WorkDatePeriod
{
    private WorkDatePeriod(WorkDate from, WorkDate through)
    {
        From = from;
        Through = through;
    }

    public WorkDate From { get; }
    public WorkDate Through { get; }

    public bool Contains(WorkDate date) =>
        date.Value >= From.Value && date.Value <= Through.Value;

    public static Result<WorkDatePeriod> Create(
        WorkDate from,
        WorkDate through)
    {
        if (from is null || through is null)
            return Result<WorkDatePeriod>.Failure(
                new Error(
                    "WorkDatePeriodRequired",
                    "Both ends of the permitted work-date period are required."));

        if (from.Value > through.Value)
            return Result<WorkDatePeriod>.Failure(
                new Error(
                    "InvalidWorkDatePeriod",
                    "The start of the permitted work-date period must not be after its end."));

        return Result<WorkDatePeriod>.Success(new WorkDatePeriod(from, through));
    }
}
