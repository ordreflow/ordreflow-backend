using Domain.Aggregate;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class RegistrationPeriodModeTests
{
    [Theory]
    [InlineData("2026-11-18", "2026-10-19", "2026-11-18")]
    [InlineData("2026-11-19", "2026-11-19", "2026-12-18")]
    [InlineData("2027-01-01", "2026-12-19", "2027-01-18")]
    public void Custom_monthly_period_includes_cutoff_and_restarts_the_following_day(
        string today, string expectedFrom, string expectedThrough)
    {
        var admin = EmployeeId.Create(Guid.NewGuid()).Value;
        var mode = RegistrationPeriodMode.Create(admin, RegistrationInterval.DayInMonth, 18).Value;

        var result = mode.GetPermittedWorkDates(DateOnly.Parse(today));

        Assert.True(result.IsSuccess);
        Assert.Equal(DateOnly.Parse(expectedFrom), result.Value.From.Value);
        Assert.Equal(DateOnly.Parse(expectedThrough), result.Value.Through.Value);
    }

    [Theory]
    [InlineData(RegistrationInterval.DayInMonth, null)]
    [InlineData(RegistrationInterval.DayInMonth, 0)]
    [InlineData(RegistrationInterval.DayInMonth, 29)]
    [InlineData(RegistrationInterval.Weekly, 18)]
    public void Invalid_cutoff_configuration_is_rejected(RegistrationInterval interval, int? cutoff)
    {
        var admin = EmployeeId.Create(Guid.NewGuid()).Value;

        var result = RegistrationPeriodMode.Create(admin, interval, cutoff);

        Assert.Equal("InvalidDayInMonth", Assert.Single(result.Errors).Code);
    }
}
