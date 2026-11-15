using Domain.Aggregate;

namespace Application.Exports;

public sealed record ExportEntryDto(Guid TimeEntryId, Guid TimeEntryAuditId);
public sealed record ExportSummaryDto(Guid Id, Guid ExportedBy, DateTime CreatedAtUtc,
    DateOnly FromDate, DateOnly ToDate, Guid? EmployeeId, int EntryCount, decimal TotalHours,
    Guid? TimeEntriesClosedBy, DateTime? TimeEntriesClosedAtUtc);
public sealed record ExportDetailsDto(ExportSummaryDto Export, IReadOnlyList<ExportEntryDto> Entries);
public sealed record ExportDownloadDto(Guid Id, DateOnly FromDate, DateOnly ToDate, byte[] Content);
public sealed record ExportEmployeeDto(Guid Id, string Name, string Status);
public sealed record ExportPage<T>(IReadOnlyList<T> Items, int Page, int PageSize);
public sealed record ExportCandidate(TimeEntry Entry, string EmployeeName, string TaskName, string CategoryName, string DepartmentName);
