namespace Application.Queries;

public sealed record ExportAcceptedTimeEntriesQuery(
    Guid RequestedBy,
    DateTime FromDate,
    DateTime ToDateExclusive);

public sealed record TimeEntryExportRow(
    Guid TimeEntryId,
    Guid EmployeeId,
    Guid TaskId,
    DateTime Date,
    decimal Hours,
    string? Comment,
    string Status);
