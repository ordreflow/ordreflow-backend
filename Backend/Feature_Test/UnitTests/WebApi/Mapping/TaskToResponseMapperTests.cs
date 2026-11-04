using System.Text.Json;
using Application.Dtos;
using WebAPI.MappingConfigurations;

namespace UnitTests.WebApi.Mapping;

public class TaskToResponseMapperTests
{
    [Fact]
    public void TaskDtoToResponse_ExposesStatusAndClosedAtInWebJson()
    {
        var closedAt = new DateTime(2026, 9, 9, 10, 30, 0, DateTimeKind.Utc);
        var dto = new TaskDto(Guid.NewGuid(), Guid.NewGuid(), "Repair", "Repair description",
            "Closed", closedAt);

        var response = new TaskToResponseMapper().Map(dto);
        var json = JsonSerializer.SerializeToElement(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(dto.Id, response.Id);
        Assert.Equal(dto.OrderId, response.OrderId);
        Assert.Equal("Closed", response.Status);
        Assert.Equal(closedAt, response.ClosedAt);
        Assert.Equal("Closed", json.GetProperty("status").GetString());
        Assert.Equal(closedAt, json.GetProperty("closedAt").GetDateTime());

        var reopened = new TaskToResponseMapper().Map(dto with { Status = "Open", ClosedAt = null });
        var reopenedJson = JsonSerializer.SerializeToElement(reopened, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("Open", reopenedJson.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, reopenedJson.GetProperty("closedAt").ValueKind);
    }
}
