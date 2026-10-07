using Domain.ValueObjects;

namespace Application.Queries;

public sealed record ExportAcceptedTimeEntriesQuery(
    UserId RequestedBy,
    DateTime FromDate,
    DateTime ToDateExclusive);

public sealed record TimeEntryExportRow(
    TimeEntryId TimeEntryId,
    UserId EmployeeId,
    TaskId TaskId,
    DateTime Date,
    decimal Hours,
    string? Comment,
    TimeEntryStatus Status);
