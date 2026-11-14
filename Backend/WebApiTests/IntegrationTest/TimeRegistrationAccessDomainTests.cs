using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace WebApiTests.IntegrationTest;

public sealed class TimeRegistrationAccessDomainTests
{
    [Fact]
    public void Public_task_without_assignments_is_available_to_active_employees()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var category = Category(internalOnly: false);
        var task = WorkTask(category.Id);

        Assert.True(category.CanRegisterTime(task, employee, false, false, false).IsSuccess);
    }

    [Fact]
    public void Assigned_task_requires_the_employee_to_be_one_of_its_assignees()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var category = Category(internalOnly: false);
        var task = WorkTask(category.Id);

        var denied = category.CanRegisterTime(task, employee, false, hasAssignments: true, isAssigned: false);

        Assert.Equal("TaskAssignmentForbidden", Assert.Single(denied.Errors).Code);
        Assert.True(category.CanRegisterTime(task, employee, false, true, true).IsSuccess);
    }

    [Fact]
    public void Internal_category_requires_department_membership_even_for_an_assignee()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var category = Category(internalOnly: true);
        var task = WorkTask(category.Id);

        var denied = category.CanRegisterTime(task, employee, false, hasAssignments: true, isAssigned: true);

        Assert.Equal("CatagoryAccessForbidden", Assert.Single(denied.Errors).Code);
        Assert.True(category.CanRegisterTime(task, employee, true, true, true).IsSuccess);
        Assert.True(category.CanRegisterTime(task, employee, true, false, false).IsSuccess);
    }

    [Fact]
    public void Inactive_employees_and_closed_work_cannot_receive_time()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var category = Category(internalOnly: false);
        var task = WorkTask(category.Id);
        employee.Status = EmployeeStatus.Deactivated;
        Assert.Equal("EmployeeAccessForbidden", Assert.Single(
            category.CanRegisterTime(task, employee, true, false, false).Errors).Code);

        employee.Status = EmployeeStatus.Active;
        category.Status = CatagoryStatus.Closed;
        Assert.Equal("CatagoryClosed", Assert.Single(
            category.CanRegisterTime(task, employee, true, false, false).Errors).Code);

        category.Status = CatagoryStatus.Active;
        task.Status = TaskStatus.Closed;
        Assert.Equal("TaskClosed", Assert.Single(
            category.CanRegisterTime(task, employee, true, false, false).Errors).Code);
    }

    [Fact]
    public void Initial_audit_allows_optional_comment_but_requires_a_duration()
    {
        var entryId = TimeEntryId.Create(Guid.NewGuid()).Value;
        var employeeId = EmployeeId.Create(Guid.NewGuid()).Value;
        var taskId = TaskId.Create(Guid.NewGuid()).Value;
        var workDate = WorkDate.Create(new DateOnly(2026, 11, 14)).Value;

        var withoutComment = TimeEntryAudit.Create(entryId, employeeId, taskId,
            workDate, Duration.Create(2.5m).Value, TimeEntryDescription.Create(null).Value, TimeEntryStatus.Draft, DateTime.UtcNow);
        var invalidDuration = TimeEntryAudit.Create(entryId, employeeId, taskId,
            workDate, null!, TimeEntryDescription.Create("Work").Value, TimeEntryStatus.Draft, DateTime.UtcNow);

        Assert.True(withoutComment.IsSuccess);
        Assert.Equal(string.Empty, withoutComment.Value.Description.Value);
        Assert.Equal("DurationRequired", Assert.Single(invalidDuration.Errors).Code);
    }

    private static Catagory Category(bool internalOnly) => new()
    {
        Id = CatagoryId.Create(Guid.NewGuid()).Value,
        DepartmentId = DepartmentId.Create(Guid.NewGuid()).Value,
        Name = CatagoryName.Create("Category").Value,
        Status = CatagoryStatus.Active,
        InternalOnly = internalOnly
    };

    private static Domain.Entities.Task WorkTask(CatagoryId categoryId) => new()
    {
        Id = TaskId.Create(Guid.NewGuid()).Value, CatagoryId = categoryId,
        Name = TaskName.Create("Work").Value, Status = TaskStatus.Active
    };
}
