namespace WebAPI.Contracts.TimeEntries;

public sealed record TimeEntryResponse(
    Guid Id,
    Guid TaskId,
    DateTime Date,
    decimal Hours,
    string? Comment,
    Guid EmployeeId,
    string Status,
    string? RejectionReason = null,
    string? EmployeeName = null,
    string? TaskName = null,
    string? CategoryName = null,
    string? DepartmentName = null);
