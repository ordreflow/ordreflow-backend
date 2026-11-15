namespace Application.Commands;

// ExportId is the stable client operation ID; a retry must reuse it.
public sealed record CreateExportCommand(Guid ExportId, DateOnly FromDate, DateOnly ToDate, Guid? EmployeeId = null);
