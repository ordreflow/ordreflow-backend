using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

/*  Summary
 *  When a time entry is created/edited, it will be stored as an TimeEntryAudit, so that the database has every
 *  version of the time entry, and is autditted, (we can see who did what and when for every time entry)
 */


public sealed class TimeEntryAudit
{
    public TimeEntryAuditId Id { get; private set; } = null!;
    
    // which timeentry is the audit for
    public TimeEntryId TimeEntryId { get; private set; } = null!;
    
    // which employee did the audit
    public EmployeeId ActorId { get; private set; } = null!;
    
    // if the timeentry is closed, then id for which employee
    public EmployeeId? ClosedBy { get; private set; }
    
    // which task do the timeentry belong to (by having it here, insted of in timeentry, employees can edit timeentries and move them to other tasks
    public TaskId TaskId { get; private set; } = null!;
    
    // the date for the time registration
    public WorkDate WorkDate { get; private set; } = null!;
    public Duration Duration { get; private set; } = null!;
    public TimeEntryDescription Description { get; private set; } = null!;
    public TimeEntryStatus Status { get; private set; }
    
    // when was the audit made
    public DateTime AuditTime { get; private set; }

    private TimeEntryAudit()
    {
    }

    public static Result<TimeEntryAudit> Create(
        TimeEntryId timeEntryId,
        EmployeeId actorId,
        TaskId taskId,
        WorkDate workDate,
        Duration duration,
        TimeEntryDescription description,
        TimeEntryStatus status,
        DateTime auditTime,
        EmployeeId? closedBy = null)
    {
        if (timeEntryId is null)
            return Result<TimeEntryAudit>.Failure(new Error("TimeEntryIdRequired", "A time entry is required."));

        if (actorId is null)
            return Result<TimeEntryAudit>.Failure(new Error("ActorRequired", "An actor is required."));

        if (taskId is null)
            return Result<TimeEntryAudit>.Failure(new Error("TaskRequired", "A task is required."));

        if (workDate is null)
            return Result<TimeEntryAudit>.Failure(new Error("WorkDateRequired", "A work date is required."));

        if (duration is null)
            return Result<TimeEntryAudit>.Failure(new Error("DurationRequired", "A duration is required."));

        if (description is null)
            return Result<TimeEntryAudit>.Failure(new Error("DescriptionRequired", "A description value is required."));

        if (!Enum.IsDefined(status))
            return Result<TimeEntryAudit>.Failure(new Error("InvalidTimeEntryStatus", "The time entry status is invalid."));

        if (auditTime == default)
            return Result<TimeEntryAudit>.Failure(new Error("AuditTimeRequired", "The audit time is required."));

        return Result<TimeEntryAudit>.Success(new TimeEntryAudit
        {
            Id = TimeEntryAuditId.Create(Guid.NewGuid()).Value,
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
