namespace Application.Queries;

public sealed record GetExportQuery(Guid ActorId, Guid ExportId);
public sealed record DownloadExportQuery(Guid ActorId, Guid ExportId);
public sealed record ListExportsQuery(Guid ActorId, int Page = 1, int PageSize = 25);
public sealed record ExportEmployeesQuery(Guid ActorId, string? Search = null, int Page = 1, int PageSize = 25);
