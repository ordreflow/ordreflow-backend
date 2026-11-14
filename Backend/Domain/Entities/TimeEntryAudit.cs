using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

/*  Summary
 *  When a time entry is created/edited, it will be stored as an TimeEntryAudit, so that the database has every
 *  version of the time entry, and is autditted, (we can see who did what and when for every time entry)
 */


public sealed class TimeEntryAudit
{
    public Guid Id { get; private set; }
    
    // which timeentry is the audit for
    public Guid TimeEntryId { get; private set; }
    
    // which employee did the audit
    public Guid ActorId { get; private set; }
    
    // if the timeentry is closed, then id for which employee
    public Guid? ClosedBy { get; private set; }
    
    // which task do the timeentry belong to (by having it here, insted of in timeentry, employees can edit timeentries and move them to other tasks
    public Guid TaskId { get; private set; }
    
    // the date for the time registration
    public DateOnly WorkDate { get; private set; }
    public double Duration { get; private set; }
    public string Description { get; private set; } = null!;
    public TimeEntryStatus Status { get; private set; }
    
    // when was the audit made
    public DateTime AuditTime { get; private set; }

    private TimeEntryAudit()
    {
    }

    public static Result<TimeEntryAudit> Create(
        Guid timeEntryId,
        Guid actorId,
        Guid taskId,
        DateOnly workDate,
        double duration,
        string description,
        TimeEntryStatus status,
        DateTime auditTime,
        Guid? closedBy = null)
    {
        if (timeEntryId == Guid.Empty)
            return Result<TimeEntryAudit>.Failure(new Error("TimeEntryIdRequired", "A time entry is required."));

        if (actorId == Guid.Empty)
            return Result<TimeEntryAudit>.Failure(new Error("ActorRequired", "An actor is required."));

        if (taskId == Guid.Empty)
            return Result<TimeEntryAudit>.Failure(new Error("TaskRequired", "A task is required."));

        if (closedBy == Guid.Empty)
            return Result<TimeEntryAudit>.Failure(new Error("InvalidCloser", "The closer ID cannot be empty."));

        if (workDate == default)
            return Result<TimeEntryAudit>.Failure(new Error("WorkDateRequired", "A work date is required."));

        if (string.IsNullOrWhiteSpace(description))
            return Result<TimeEntryAudit>.Failure(new Error("DescriptionRequired", "A description is required."));

        if (!Enum.IsDefined(status))
            return Result<TimeEntryAudit>.Failure(new Error("InvalidTimeEntryStatus", "The time entry status is invalid."));

        if (auditTime == default)
            return Result<TimeEntryAudit>.Failure(new Error("AuditTimeRequired", "The audit time is required."));

        return Result<TimeEntryAudit>.Success(new TimeEntryAudit
        {
            Id = Guid.NewGuid(),
            TimeEntryId = timeEntryId,
            ActorId = actorId,
            ClosedBy = closedBy,
            TaskId = taskId,
            WorkDate = workDate,
            Duration = duration,
            Description = description,
            Status = status,
            AuditTime = auditTime
        });
    }
}
