using Domain.Aggregate;
using Domain.Services;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

// UC-09 (FR-09, FR-21): selectable work must agree with time-registration access rules in every case.
public sealed class WorkSelectionRulesTests
{
    public static IEnumerable<object[]> AllCases()
    {
        bool[] values = [false, true];
        foreach (var internalOnly in values)
        foreach (var assignmentOnly in values)
        foreach (var member in values)
        foreach (var hasAssignments in values)
        foreach (var isAssigned in values)
        foreach (var closed in new[] { "none", "category", "task" })
        {
            if (isAssigned && !hasAssignments) continue; // An assignee implies that the task has assignments.
            yield return [internalOnly, assignmentOnly, member, hasAssignments, isAssigned, closed];
        }
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void Selectable_work_is_exactly_the_work_time_can_be_registered_on(
        bool internalOnly, bool assignmentOnly, bool member, bool hasAssignments, bool isAssigned, string closed)
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var department = Department.Create(DepartmentName.Create("Service").Value).Value;
        var category = department.AddCatagory(EmployeeId.Create(Guid.NewGuid()).Value,
            CatagoryName.Create("Customers").Value, internalOnly, DateTime.UtcNow).Value;
        var task = department.AddTask(category.Id, TaskName.Create("Work").Value, DateTime.UtcNow, null,
            WorkItemKind.Task, assignmentOnly).Value;
        if (closed == "category") Assert.True(department.CloseCatagory(category.Id, DateTime.UtcNow.AddMinutes(1)).IsSuccess);
        if (closed == "task") Assert.True(department.CloseTask(category.Id, task.Id, DateTime.UtcNow.AddMinutes(1)).IsSuccess);

        var selectable = WorkSelectionRules.IsSelectable(category, task, member, hasAssignments, isAssigned);
        var date = WorkDate.Create(new DateOnly(2026, 11, 14)).Value;
        var registered = new TimeRegistrationDomainService().Register(employee, department, task.Id, date,
            Duration.Create(1m).Value, TimeEntryDescription.Create(null).Value,
            new TimeRegistrationAccess(member, hasAssignments, isAssigned), WorkDatePeriod.Create(date, date).Value, DateTime.UtcNow);

        Assert.Equal(registered.IsSuccess, selectable);
    }
}
