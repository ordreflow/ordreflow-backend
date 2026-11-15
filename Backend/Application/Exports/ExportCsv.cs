using System.Globalization;
using System.Text;

namespace Application.Exports;

public static class ExportCsv
{
    public const string Header = "Date,Employee,Department,Category,Task,Hours,Comment,Status,EmployeeId,TaskId,TimeEntryId";

    public static byte[] Create(IEnumerable<ExportCandidate> entries)
    {
        var csv = new StringBuilder(Header).Append("\r\n");
        foreach (var row in entries)
        {
            var audit = row.Entry.GetCurrentAudit()!;
            csv.AppendJoin(',', audit.WorkDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Field(row.EmployeeName), Field(row.DepartmentName), Field(row.CategoryName), Field(row.TaskName),
                audit.Duration.Value.ToString("0.##", CultureInfo.InvariantCulture), Field(audit.Description.Value),
                audit.Status.ToString(), row.Entry.EmployeeId.Value, audit.TaskId.Value, row.Entry.Id.Value).Append("\r\n");
        }
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())];
    }

    private static string Field(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n') value = "'" + value;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
