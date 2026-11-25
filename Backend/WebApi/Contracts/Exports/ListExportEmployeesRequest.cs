namespace WebAPI.Contracts.Exports;

public sealed record ListExportEmployeesRequest(string? Search = null, int Page = 1, int PageSize = 25);
