using Application.Exports;

namespace WebAPI.Contracts.Exports;

public sealed record ExportDetailsResponse(ExportSummaryDto Export, IReadOnlyList<ExportEntryDto> Entries, string DownloadUrl);
