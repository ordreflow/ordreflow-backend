using Application.Queries;
using Domain.ValueObjects;

namespace UnitTests.Features.FR15;

public class EmployeeWeeklyTotalAggregationTests
{
    private static readonly DateTime Monday = new DateOnly(2026, 9, 7).ToDateTime(TimeOnly.MinValue);
    private static readonly DateTime NextMonday = new DateOnly(2026, 9, 14).ToDateTime(TimeOnly.MinValue);

    // All fractional hours in the selected week are added without rounding to whole hours.
    [Fact]
    public async Task MultipleEntries_ReturnExactDecimalSum()
    {
        var employeeId = EmployeeWeeklyTotalTestData.CreateEmployeeId();
        var first = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday, 1.25m);
        var second = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday.AddDays(2), 2.5m);
        var third = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday.AddDays(4), 3.12m);
        var (handler, _) = EmployeeWeeklyTotalTestData.CreateHandler(first, second, third);

        var result = await handler.HandleAsync(new GetMyWeeklyTotalQuery(
            employeeId.Value, Monday, NextMonday));

        Assert.True(result.IsSuccess);
        Assert.Equal(6.87m, result.Value);
    }

    // No pre-scoped entries produce a zero total and the employee ID is forwarded once.
    [Fact]
    public async Task EmptyWeek_ReturnsZeroAndForwardsEmployeeIdOnce()
    {
        var employeeId = EmployeeWeeklyTotalTestData.CreateEmployeeId();
        var (handler, repository) = EmployeeWeeklyTotalTestData.CreateHandler();

        var result = await handler.HandleAsync(new GetMyWeeklyTotalQuery(
            employeeId.Value, Monday, NextMonday));

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
        Assert.Equal(employeeId, Assert.Single(repository.RequestedEmployeeIds));
    }

    // The calendar week counts Monday at midnight through Sunday, excluding both adjacent days.
    [Fact]
    public async Task CalendarWeek_IncludesStartAndLastDayButExcludesNextMonday()
    {
        var employeeId = EmployeeWeeklyTotalTestData.CreateEmployeeId();
        var before = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday.AddDays(-1), 24m);
        var atStart = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday, 1.25m);
        var onSunday = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, NextMonday.AddDays(-1), 3.75m);
        var atEnd = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, NextMonday, 24m);
        var (handler, _) = EmployeeWeeklyTotalTestData.CreateHandler(before, atStart, onSunday, atEnd);

        var result = await handler.HandleAsync(new GetMyWeeklyTotalQuery(
            employeeId.Value, Monday, NextMonday));

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value);
    }

    // Draft, accepted, returned, and finalized entries all contribute to the current total.
    [Fact]
    public async Task EntriesInAllStatuses_AreCounted()
    {
        var employeeId = EmployeeWeeklyTotalTestData.CreateEmployeeId();
        var managerId = UserId.Create(Guid.Parse("8b81e7be-7db8-424e-bb9f-4ef111ec9282")).Value;
        var draft = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday, 1.25m);
        var accepted = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday.AddDays(1), 2.5m);
        var returned = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday.AddDays(2), 3.75m);
        var finalized = EmployeeWeeklyTotalTestData.CreateEntry(employeeId, Monday.AddDays(3), 4.12m);
        Assert.True(accepted.Accept(managerId, employeeId, managerId,
            UserRole.Manager, UserStatus.Active).IsSuccess);
        Assert.True(returned.Return(managerId, employeeId, managerId,
            UserRole.Manager, UserStatus.Active, "Needs correction").IsSuccess);
        Assert.True(finalized.Accept(managerId, employeeId, managerId,
            UserRole.Manager, UserStatus.Active).IsSuccess);
        Assert.True(finalized.Finalize(managerId, employeeId, managerId,
            UserRole.Manager, UserStatus.Active).IsSuccess);
        Assert.Equal(TimeEntryStatus.Draft, draft.Status);
        Assert.Equal(TimeEntryStatus.Accepted, accepted.Status);
        Assert.Equal(TimeEntryStatus.Returned, returned.Status);
        Assert.Equal(TimeEntryStatus.Finalized, finalized.Status);
        var (handler, _) = EmployeeWeeklyTotalTestData.CreateHandler(draft, accepted, returned, finalized);

        var result = await handler.HandleAsync(new GetMyWeeklyTotalQuery(
            employeeId.Value, Monday, NextMonday));

        Assert.True(result.IsSuccess);
        Assert.Equal(11.62m, result.Value);
    }
}
