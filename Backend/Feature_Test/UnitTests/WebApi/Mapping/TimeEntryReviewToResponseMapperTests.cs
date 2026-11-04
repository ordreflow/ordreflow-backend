using System.Text.Json;
using Application.Dtos;
using WebAPI.Contracts.TimeEntries;
using WebAPI.MappingConfigurations;

namespace UnitTests.WebApi.Mapping;

public class TimeEntryReviewToResponseMapperTests
{
    [Fact]
    public void ResponseMapper_PreservesReviewDetailsInWebJson()
    {
        var dto = new TimeEntryReviewDto(
            Guid.Parse("479be205-6659-4a49-87c9-61b693c4f7e0"),
            Guid.Parse("34841e23-0915-452d-a5a8-44f7cb2a01d4"),
            "Returned", "Needs correction", new DateTime(2026, 9, 9, 10, 30, 0, DateTimeKind.Utc));
        var accepted = dto with { Id = Guid.Parse("d09ad2eb-59f9-4f2f-a435-3ff06b05e363"),
            Decision = "Accepted", Reason = null };
        var mapper = new TimeEntryReviewToResponseMapper();
        var response = new GetTimeEntryReviewsResponse(new[] { mapper.Map(dto), mapper.Map(accepted) });

        var json = JsonSerializer.SerializeToElement(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var items = json.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(dto.Id, items[0].GetProperty("id").GetGuid());
        Assert.Equal(dto.TimeEntryId, items[0].GetProperty("timeEntryId").GetGuid());
        Assert.Equal("Returned", items[0].GetProperty("decision").GetString());
        Assert.Equal("Needs correction", items[0].GetProperty("reason").GetString());
        Assert.Equal(dto.ReviewedAt, items[0].GetProperty("reviewedAt").GetDateTime());
        Assert.Equal(accepted.Id, items[1].GetProperty("id").GetGuid());
        Assert.Equal("Accepted", items[1].GetProperty("decision").GetString());
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("reason").ValueKind);
    }
}
