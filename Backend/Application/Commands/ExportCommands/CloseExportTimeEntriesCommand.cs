namespace Application.Commands;

public sealed record CloseExportTimeEntriesCommand(Guid RequestedBy, Guid ExportId) : ITransactionalCommand
{
    public string ConflictCode => "ExportClosureConflict";
}
