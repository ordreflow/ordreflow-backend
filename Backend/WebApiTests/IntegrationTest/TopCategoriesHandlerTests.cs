using Application.Dtos;
using Application.Handlers;
using Application.Interfaces;
using Application.Queries;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class TopCategoriesHandlerTests
{
    private static readonly DateOnly From = new(2026, 11, 1);
    private static readonly DateOnly To = new(2026, 11, 30);

    [Fact]
    public async Task Empty_actor_fails_before_any_lookup_and_unknown_actor_cannot_query_report()
    {
        var employees = new Employees();
        var reports = new Reports();
        var handler = new GetTopCategoriesHandler(reports, employees);
        var empty = await handler.HandleAsync(new(Guid.Empty, From, To));
        Assert.Equal("EmployeeIdEmpty", Assert.Single(empty.Errors).Code);
        Assert.Equal(0, employees.Reads);
        var unknown = await handler.HandleAsync(new(Guid.NewGuid(), From, To));
        Assert.Equal("ReportForbidden", Assert.Single(unknown.Errors).Code);
        Assert.Equal(0, reports.Reads);
    }

    [Theory]
    [InlineData(SystemPermission.Employee, true, true)]
    [InlineData(SystemPermission.Payroll, true, true)]
    [InlineData(SystemPermission.CatagoryReviewer, true, true)]
    [InlineData(SystemPermission.DepartmentManager, false, true)]
    [InlineData(SystemPermission.Administrator, false, true)]
    [InlineData(SystemPermission.DepartmentManager, true, false)]
    public async Task Unauthorized_or_inactive_actor_cannot_read_totals(SystemPermission permission, bool active, bool baseline)
    {
        var actor = Actor(permission);
        Assert.True(actor.ChangeProfile(actor.Name, actor.Email, "Manager").IsSuccess);
        if (!baseline) Assert.True(actor.RevokePermission(SystemPermission.Employee).IsSuccess);
        if (!active) actor.Deactivate();
        var reports = new Reports();

        var result = await new GetTopCategoriesHandler(reports, new Employees(actor)).HandleAsync(new(actor.Id.Value, From, To));

        Assert.Equal("ReportForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, reports.Reads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Manager_scope_is_actor_identity_and_administrator_scope_is_company_wide(bool administrator)
    {
        var actor = Actor(SystemPermission.DepartmentManager);
        if (administrator) Assert.True(actor.GrantPermission(SystemPermission.Administrator).IsSuccess);
        var reports = new Reports();
        var result = await new GetTopCategoriesHandler(reports, new Employees(actor)).HandleAsync(new(actor.Id.Value, From, To));

        Assert.True(result.IsSuccess);
        Assert.Same(reports.Rows, result.Value);
        Assert.Equal(administrator ? null : actor.Id, reports.ManagerId);
        Assert.Equal(From, reports.From);
        Assert.Equal(To, reports.To);
        Assert.Equal(1, reports.Reads);
    }

    [Fact]
    public async Task Invalid_dates_fail_before_report_query_but_single_day_and_maximum_end_are_valid()
    {
        var actor = Actor(SystemPermission.DepartmentManager);
        var reports = new Reports();
        var handler = new GetTopCategoriesHandler(reports, new Employees(actor));
        var missing = await handler.HandleAsync(new(actor.Id.Value, default, To));
        var reversed = await handler.HandleAsync(new(actor.Id.Value, To, From));
        Assert.Equal("InvalidDateRange", Assert.Single(missing.Errors).Code);
        Assert.Equal("InvalidDateRange", Assert.Single(reversed.Errors).Code);
        Assert.Equal(0, reports.Reads);
        Assert.True((await handler.HandleAsync(new(actor.Id.Value, From, From))).IsSuccess);
        Assert.Equal(From, reports.To);
        Assert.True((await handler.HandleAsync(new(actor.Id.Value, From, DateOnly.MaxValue))).IsSuccess);
        Assert.Equal(DateOnly.MaxValue, reports.To);
    }

    private static Employee Actor(SystemPermission permission)
    {
        var actor = Employee.Create("User", $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
        Assert.True(actor.GrantPermission(permission).IsSuccess);
        return actor;
    }
    private sealed class Employees(Employee? employee = null) : IEmployeeAccessRepository
    {
        public int Reads { get; private set; }
        public Task<Employee?> GetWithAccessesAsync(EmployeeId id, CancellationToken cancellationToken = default)
        {
            Reads++;
            return Task.FromResult(employee?.Id == id ? employee : null);
        }
    }
    private sealed class Reports : ITopCategoriesQuery
    {
        public IReadOnlyList<TopCategoryReportDto> Rows { get; } =
            [new(Guid.NewGuid(), "Category", Guid.NewGuid(), "Department", 7.5m, [new(Guid.NewGuid(), "Task", 7.5m)])];
        public int Reads { get; private set; }
        public EmployeeId? ManagerId { get; private set; }
        public DateOnly From { get; private set; }
        public DateOnly To { get; private set; }
        public Task<IReadOnlyList<TopCategoryReportDto>> GetAsync(EmployeeId? managerId, DateOnly fromDate,
            DateOnly toDate, CancellationToken cancellationToken = default)
        {
            Reads++; ManagerId = managerId; From = fromDate; To = toDate;
            return Task.FromResult(Rows);
        }
    }
}
