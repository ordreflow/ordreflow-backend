using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class DurationTests
{
    [Fact]
    public void Quarter_hours_and_two_day_lengths_are_permitted()
    {
        foreach (var hours in new[] { 0.25m, 0.5m, 0.75m, 1m, 1.25m, 7.4m, 3.7m, 24m })
            Assert.Equal(hours, Duration.Create(hours).Value.Value);
    }

    [Fact]
    public void Other_fractional_hours_are_rejected()
    {
        foreach (var hours in new[] { 0.1m, 3.71m, 7.41m, 23.99m })
        {
            var result = Duration.Create(hours);
            Assert.Equal("InvalidDurationIncrement", Assert.Single(result.Errors).Code);
        }
    }

    [Fact]
    public void Duration_still_has_to_be_greater_than_zero_and_no_more_than_24_hours()
    {
        foreach (var hours in new[] { -0.25m, 0m, 24.25m })
        {
            var result = Duration.Create(hours);
            Assert.Equal("InvalidDuration", Assert.Single(result.Errors).Code);
        }
    }
}
