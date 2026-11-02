namespace Application.Queries;

public sealed record ExportAcceptedTimeEntriesQuery(
    Guid RequestedBy,
    DateTime FromDate,
    DateTime ToDateExclusive,
    Guid? EmployeeId = null);

public sealed record TimeEntryExportRow(
    Guid TimeEntryId,
    Guid EmployeeId,
    string EmployeeName,
    Guid TaskId,
    string TaskTitle,
    string OrderName,
    DateTime Date,
    decimal Hours,
    string? Comment,
    string Status);
