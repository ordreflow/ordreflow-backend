using Application.Queries;

namespace UnitTests.Features.FR04;

public class EmployeeWeekEntriesSelectionTests
{
    private static readonly DateTime Monday = new DateOnly(2026, 9, 7).ToDateTime(TimeOnly.MinValue);
    private static readonly DateTime NextMonday = new DateOnly(2026, 9, 14).ToDateTime(TimeOnly.MinValue);

    // An employee with no pre-scoped entries gets an empty list, and the requested ID is forwarded once.
    [Fact]
    public async Task EmptyWeek_ReturnsEmptyAndForwardsEmployeeIdOnce()
    {
        var employeeId = EmployeeWeekEntriesTestData.CreateEmployeeId();
        var (handler, repository) = EmployeeWeekEntriesTestData.CreateHandler();

        var result = await handler.HandleAsync(new GetMyTimeEntriesForWeekQuery(
            employeeId.Value, Monday, NextMonday));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(employeeId, Assert.Single(repository.RequestedEmployeeIds));
    }

    // The calendar week includes Monday at midnight and all of Sunday, but not either adjacent day.
    [Fact]
    public async Task CalendarWeek_IncludesStartAndLastDayButExcludesNextMonday()
    {
        var employeeId = EmployeeWeekEntriesTestData.CreateEmployeeId();
        var before = EmployeeWeekEntriesTestData.CreateEntry(employeeId, Monday.AddTicks(-1));
        var atStart = EmployeeWeekEntriesTestData.CreateEntry(employeeId, Monday);
        var onSunday = EmployeeWeekEntriesTestData.CreateEntry(employeeId, NextMonday.AddTicks(-1));
        var atEnd = EmployeeWeekEntriesTestData.CreateEntry(employeeId, NextMonday);
        var (handler, _) = EmployeeWeekEntriesTestData.CreateHandler(before, atEnd, onSunday, atStart);

        var result = await handler.HandleAsync(new GetMyTimeEntriesForWeekQuery(
            employeeId.Value, Monday, NextMonday));

        Assert.True(result.IsSuccess);
        Assert.Equal([atStart.Id.Value, onSunday.Id.Value], result.Value.Select(dto => dto.Id));
    }

    // An empty employee ID fails validation without reading entries.
    [Fact]
    public async Task EmptyEmployeeId_ReturnsUserIdEmptyWithoutRepositoryCall()
    {
        var (handler, repository) = EmployeeWeekEntriesTestData.CreateHandler();

        var result = await handler.HandleAsync(new GetMyTimeEntriesForWeekQuery(
            Guid.Empty, Monday, NextMonday));

        Assert.True(result.IsFailure);
        Assert.Equal("UserIdEmpty", Assert.Single(result.Errors).Code);
        Assert.Empty(repository.RequestedEmployeeIds);
    }

    // An equal or earlier exclusive end date fails before the repository is read.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidWeekRange_ReturnsInvalidDateRangeWithoutRepositoryCall(int endOffsetDays)
    {
        var employeeId = EmployeeWeekEntriesTestData.CreateEmployeeId();
        var (handler, repository) = EmployeeWeekEntriesTestData.CreateHandler();

        var result = await handler.HandleAsync(new GetMyTimeEntriesForWeekQuery(
            employeeId.Value, Monday, Monday.AddDays(endOffsetDays)));

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidDateRange", Assert.Single(result.Errors).Code);
        Assert.Empty(repository.RequestedEmployeeIds);
    }
}
