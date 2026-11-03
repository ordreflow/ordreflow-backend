using System.Text.Json;
using Application.Dtos;
using WebAPI.MappingConfigurations;

namespace UnitTests.WebApi.Mapping;

public class TimeEntryToResponseMapperTests
{
    [Fact]
    public void TimeEntryToResponseMapper_ExposesEmployeeIdAndStatusInWebJson()
    {
        var employeeId = Guid.Parse("e47b48ba-01cf-4c2b-902a-c57af409622c");
        var dto = new TimeEntryDto(
            Id: Guid.Parse("1a20de36-e6ba-48d8-8271-2322461874a6"),
            TaskId: Guid.Parse("d35e54f5-814c-4745-9893-c1c088997f2e"),
            Date: new DateTime(2026, 9, 9),
            Hours: 7.5m,
            Comment: "Completed work",
            EmployeeId: employeeId,
            Status: "Returned");

        var response = new TimeEntryToResponseMapper().Map(dto);

        Assert.Equal(employeeId, response.EmployeeId);
        Assert.Equal("Returned", response.Status);
        var json = JsonSerializer.SerializeToElement(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(employeeId, json.GetProperty("employeeId").GetGuid());
        Assert.Equal("Returned", json.GetProperty("status").GetString());
    }
}
