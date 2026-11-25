using Application.Exports;
using WebAPI.Contracts.Exports;

namespace WebAPI.Endpoints.Exports;

internal static class ExportResponses
{
    public static ExportDetailsResponse ToResponse(ExportDetailsDto details) =>
        new(details.Export, details.Entries, $"/api/exports/{details.Export.Id}/download");
}
