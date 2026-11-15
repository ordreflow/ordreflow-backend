using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.Services;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class TimeRegistrationAccessDomainTests
{
    [Fact]
    public void General_work_does_not_require_assignment_or_reviewer_permission()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var (department, category, task) = Work(false);
        Assert.False(employee.HasPermission(SystemPermission.CatagoryReviewer));
        Assert.True(Register(employee, department, task.Id, false, false, false).IsSuccess);
    }

    [Fact]
    public void Assigned_work_requires_assignment_and_internal_work_also_requires_membership()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var (department, category, task) = Work(true);
        Assert.Equal("CatagoryAccessForbidden", Assert.Single(Register(employee, department, task.Id, false, true, true).Errors).Code);
        Assert.Equal("TaskAssignmentForbidden", Assert.Single(Register(employee, department, task.Id, true, true, false).Errors).Code);
        Assert.True(Register(employee, department, task.Id, true, true, true).IsSuccess);
        Assert.True(Register(employee, department, task.Id, true, false, false).IsSuccess);
    }

    [Fact]
    public void Inactive_employee_closed_category_and_closed_task_are_rejected()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var (department, category, task) = Work(false);
        employee.Deactivate();
        Assert.Equal("EmployeeAccessForbidden", Assert.Single(Register(employee, department, task.Id, true, false, false).Errors).Code);
        employee.Activate();
        Assert.True(department.CloseCatagory(category.Id, DateTime.UtcNow.AddMinutes(1)).IsSuccess);
        Assert.Equal("CatagoryClosed", Assert.Single(Register(employee, department, task.Id, true, false, false).Errors).Code);
        Assert.True(department.ReopenCatagory(category.Id).IsSuccess);
        Assert.True(department.CloseTask(category.Id, task.Id, DateTime.UtcNow.AddMinutes(1)).IsSuccess);
        Assert.Equal("TaskClosed", Assert.Single(Register(employee, department, task.Id, true, false, false).Errors).Code);
    }

    [Fact]
    public void Root_creates_child_identities_and_does_not_accept_foreign_category_ids()
    {
        var (department, category, task) = Work(false);
        var other = Department.Create(DepartmentName.Create("Other").Value).Value;
        Assert.Equal(department.Id, category.DepartmentId);
        Assert.Equal(category.Id, task.CatagoryId);
        Assert.Equal("CatagoryNotFound", Assert.Single(other.AddTask(category.Id,
            TaskName.Create("Foreign").Value, DateTime.UtcNow).Errors).Code);
        Assert.Equal("TaskNotFound", Assert.Single(other.CanRegisterTime(task.Id).Errors).Code);
    }

    [Fact]
    public void Domain_service_enforces_the_date_period()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var (department, category, task) = Work(false);
        var date = WorkDate.Create(new DateOnly(2026, 11, 14)).Value;
        var next = WorkDate.Create(date.Value.AddDays(1)).Value;
        var result = new TimeRegistrationDomainService().Register(employee, department, task.Id, date,
            Duration.Create(2.5m).Value, TimeEntryDescription.Create(null).Value,
            new TimeRegistrationAccess(false, false, false), WorkDatePeriod.Create(next, next).Value, DateTime.UtcNow);
        Assert.Equal("WorkDateNotPermitted", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Initial_audit_allows_optional_comment_but_requires_duration()
    {
        var id = TimeEntryId.Create(Guid.NewGuid()).Value;
        var employee = EmployeeId.Create(Guid.NewGuid()).Value;
        var task = TaskId.Create(Guid.NewGuid()).Value;
        var date = WorkDate.Create(new DateOnly(2026, 11, 14)).Value;
        var valid = TimeEntryAudit.Create(id, employee, task, date, Duration.Create(2.5m).Value,
            TimeEntryDescription.Create(null).Value, TimeEntryStatus.ReadyForApproval, DateTime.UtcNow);
        var invalid = TimeEntryAudit.Create(id, employee, task, date, null!,
            TimeEntryDescription.Create(null).Value, TimeEntryStatus.ReadyForApproval, DateTime.UtcNow);
        Assert.Equal(string.Empty, valid.Value.Description.Value);
        Assert.Equal("DurationRequired", Assert.Single(invalid.Errors).Code);
    }

    private static (Department Department, Catagory Category, Domain.Entities.Task Task) Work(bool internalOnly)
    {
        var department = Department.Create(DepartmentName.Create("Department").Value).Value;
        var category = department.AddCatagory(EmployeeId.Create(Guid.NewGuid()).Value,
            CatagoryName.Create("Category").Value, internalOnly, DateTime.UtcNow).Value;
        return (department, category, department.AddTask(category.Id, TaskName.Create("Work").Value, DateTime.UtcNow).Value);
    }

    private static Result<TimeEntry> Register(Employee employee, Department department, TaskId task,
        bool member, bool assignedWork, bool assignee)
    {
        var date = WorkDate.Create(new DateOnly(2026, 11, 14)).Value;
        return new TimeRegistrationDomainService().Register(employee, department, task, date,
            Duration.Create(2.5m).Value, TimeEntryDescription.Create(null).Value,
            new TimeRegistrationAccess(member, assignedWork, assignee), WorkDatePeriod.Create(date, date).Value, DateTime.UtcNow);
    }
}
