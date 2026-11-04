using Application.Queries;

namespace UnitTests.Features.FR15;

public class EmployeeWeeklyTotalValidationTests
{
    private static readonly DateTime Monday = new DateOnly(2026, 9, 7).ToDateTime(TimeOnly.MinValue);
    private static readonly DateTime NextMonday = new DateOnly(2026, 9, 14).ToDateTime(TimeOnly.MinValue);

    // An empty employee ID fails validation without consulting the repository.
    [Fact]
    public async Task EmptyEmployeeId_ReturnsUserIdEmptyWithoutRepositoryCall()
    {
        var (handler, repository) = EmployeeWeeklyTotalTestData.CreateHandler();

        var result = await handler.HandleAsync(new GetMyWeeklyTotalQuery(
            Guid.Empty, Monday, NextMonday));

        Assert.True(result.IsFailure);
        Assert.Equal("UserIdEmpty", Assert.Single(result.Errors).Code);
        Assert.Empty(repository.RequestedEmployeeIds);
    }

    // An equal or earlier exclusive end date fails before entries are fetched.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidWeekRange_ReturnsInvalidDateRangeWithoutRepositoryCall(int endOffsetDays)
    {
        var employeeId = EmployeeWeeklyTotalTestData.CreateEmployeeId();
        var (handler, repository) = EmployeeWeeklyTotalTestData.CreateHandler();

        var result = await handler.HandleAsync(new GetMyWeeklyTotalQuery(
            employeeId.Value, Monday, Monday.AddDays(endOffsetDays)));

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidDateRange", Assert.Single(result.Errors).Code);
        Assert.Empty(repository.RequestedEmployeeIds);
    }
}
