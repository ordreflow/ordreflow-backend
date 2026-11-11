using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class TimeEntryAuditEvent
{
    public TimeEntryAuditEventId Id { get; private set; } = null!;
    public TimeEntryId TimeEntryId { get; private set; } = null!;
    public UserId ActorId { get; private set; } = null!;
    public TimeEntryAuditEventType EventType { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string RelevantChanges { get; private set; } = null!;

    private TimeEntryAuditEvent()
    {
    }

    private TimeEntryAuditEvent(
        TimeEntryId timeEntryId,
        UserId actorId,
        TimeEntryAuditEventType eventType,
        string relevantChanges)
    {
        Id = TimeEntryAuditEventId.Create(Guid.NewGuid()).Value;
        TimeEntryId = timeEntryId;
        ActorId = actorId;
        EventType = eventType;
        OccurredAt = DateTime.UtcNow;
        RelevantChanges = relevantChanges;
    }

    public static Result<TimeEntryAuditEvent> Create(
        TimeEntryId timeEntryId,
        UserId actorId,
        TimeEntryAuditEventType eventType,
        string relevantChanges)
    {
        if (timeEntryId is null)
            return Result<TimeEntryAuditEvent>.Failure(new Error("TimeEntryRequired", "A time entry is required."));

        if (actorId is null)
            return Result<TimeEntryAuditEvent>.Failure(new Error("ActorRequired", "An event actor is required."));

        if (!Enum.IsDefined(eventType))
            return Result<TimeEntryAuditEvent>.Failure(new Error("InvalidAuditEventType", "The audit event type is invalid."));

        if (string.IsNullOrWhiteSpace(relevantChanges))
            return Result<TimeEntryAuditEvent>.Failure(new Error("RelevantChangesRequired", "Relevant changes are required."));

        relevantChanges = relevantChanges.Trim();
        return relevantChanges.Length > 4000
            ? Result<TimeEntryAuditEvent>.Failure(new Error("RelevantChangesTooLong", "Relevant changes cannot exceed 4000 characters."))
            : Result<TimeEntryAuditEvent>.Success(new TimeEntryAuditEvent(
                timeEntryId,
                actorId,
                eventType,
                relevantChanges));
    }
}
