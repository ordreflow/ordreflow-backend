using System.Text;
using Application.Queries;
using WebAPI.Common;

namespace UnitTests.WebApi.Formatting;

public class TimeEntryExportCsvTests
{
    [Fact]
    public void Csv_HasBomHeaderInvariantNumbersAndIsoDates()
    {
        var bytes = TimeEntryExportCsv.Create([Row(hours: 7.5m, comment: "Done")]);

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n");
        Assert.Equal(TimeEntryExportCsv.Header, lines[0]);
        Assert.StartsWith("2026-10-05,Anna Andersen,Netto Vestergade,Opsæt reoler,7.5,Done,Accepted,", lines[1]);
        Assert.Equal("", lines[2]);
    }

    [Fact]
    public void Csv_QuotesSpecialCharactersAndNeutralizesFormulas()
    {
        var bytes = TimeEntryExportCsv.Create([Row(hours: 1m, comment: "Line \"one\",\nline two"),
                                               Row(hours: 1m, comment: "=HYPERLINK(\"x\")")]);

        var text = Encoding.UTF8.GetString(bytes[3..]);
        Assert.Contains(",\"Line \"\"one\"\",\nline two\",", text);
        Assert.Contains(",\"'=HYPERLINK(\"\"x\"\")\",", text);
    }

    private static TimeEntryExportRow Row(decimal hours, string? comment) => new(
        Guid.NewGuid(), Guid.NewGuid(), "Anna Andersen", Guid.NewGuid(), "Opsæt reoler", "Netto Vestergade",
        new DateTime(2026, 10, 5), hours, comment, "Accepted");
}
