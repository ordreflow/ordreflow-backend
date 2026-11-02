using System.Globalization;
using System.Text;
using Application.Queries;

namespace WebAPI.Common;

// Builds the accepted time entries export as RFC 4180 CSV (comma separated, invariant numbers).
public static class TimeEntryExportCsv
{
    public const string Header =
        "Date,Employee,Order,Task,Hours,Comment,Status,EmployeeId,TaskId,TimeEntryId";

    public static byte[] Create(IEnumerable<TimeEntryExportRow> rows)
    {
        var csv = new StringBuilder();
        csv.Append(Header).Append("\r\n");

        foreach (var row in rows)
        {
            csv.AppendJoin(',',
                    row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Field(row.EmployeeName),
                    Field(row.OrderName),
                    Field(row.TaskTitle),
                    row.Hours.ToString("0.##", CultureInfo.InvariantCulture),
                    Field(row.Comment),
                    row.Status,
                    row.EmployeeId,
                    row.TaskId,
                    row.TimeEntryId)
                .Append("\r\n");
        }

        // The UTF-8 BOM makes Excel show æ, ø and å correctly.
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())];
    }

    private static string Field(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Text starting with these characters is run as a formula by spreadsheets (CSV injection).
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;

        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
