namespace WebAPI.Contracts.Exports;

public sealed record CreateExportRequest(Guid ExportId, DateOnly FromDate, DateOnly ToDate, Guid? EmployeeId = null);
