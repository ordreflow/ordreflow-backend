using System.Reflection;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.Services;
using Domain.ValueObjects;
using WorkTask = Domain.Entities.Task;

namespace WebApiTests.IntegrationTest;

// UC-08 (FR-08, SEC-08): who may plan work where, and how assignment-only work is protected.
public sealed class WorkPlanningDomainTests
{
    private readonly WorkPlanningDomainService _planning = new();

    private static Department NewDepartment(string name = "Service") => Department.Create(DepartmentName.Create(name).Value).Value;

    private static Employee NewEmployee(params SystemPermission[] permissions)
    {
        var employee = Employee.Create("Morgan Manager", $"morgan-{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
        foreach (var permission in permissions)
            Assert.True(employee.GrantPermission(permission).IsSuccess);
        return employee;
    }

    private Result<Catagory> Category(Employee actor, Department department, params DepartmentId[] managed) =>
        _planning.CreateCatagory(actor, department, managed, CatagoryName.Create("Customers").Value, false, null, DateTime.UtcNow);

    [Fact]
    public void Manager_plans_projects_tickets_and_tasks_in_a_managed_department()
    {
        var department = NewDepartment();
        var manager = NewEmployee(SystemPermission.DepartmentManager);
        var category = Category(manager, department, department.Id).Value;

        var project = _planning.CreateWork(manager, department, [department.Id], category.Id, TaskName.Create("New site").Value,
            "Build it", WorkItemKind.Project, assignmentOnly: false, DateTime.UtcNow);
        var ticket = _planning.CreateWork(manager, department, [department.Id], category.Id, TaskName.Create("Fix door").Value,
            null, WorkItemKind.Ticket, assignmentOnly: true, DateTime.UtcNow);

        Assert.Equal((WorkItemKind.Project, false), (project.Value.Kind, project.Value.AssignmentOnly));
        Assert.Equal((WorkItemKind.Ticket, true), (ticket.Value.Kind, ticket.Value.AssignmentOnly));
        Assert.Equal(manager.Id, category.CreatedBy);
        Assert.Equal(2, category.Tasks.Count);
        Assert.All(category.Tasks, task => Assert.Equal(category.Id, task.CatagoryId));
    }

    [Fact]
    public void Manager_cannot_plan_in_a_department_they_do_not_manage()
    {
        var mine = NewDepartment("Mine");
        var other = NewDepartment("Other");
        var otherCategory = Category(NewEmployee(SystemPermission.Administrator), other).Value;
        var manager = NewEmployee(SystemPermission.DepartmentManager);

        Assert.Equal("WorkPlanningForbidden", Assert.Single(Category(manager, other, mine.Id).Errors).Code);
        Assert.Equal("WorkPlanningForbidden", Assert.Single(_planning.CreateWork(manager, other, [mine.Id], otherCategory.Id,
            TaskName.Create("Sneaky").Value, null, WorkItemKind.Task, false, DateTime.UtcNow).Errors).Code);
        Assert.Empty(otherCategory.Tasks);
    }

    [Fact]
    public void Link_without_grant_or_title_without_grant_gives_no_planning_access()
    {
        var department = NewDepartment();
        var linkedWithoutGrant = NewEmployee();
        var titled = NewEmployee();
        Assert.True(titled.ChangeProfile(titled.Name, titled.Email, "Department manager").IsSuccess);

        Assert.Equal("WorkPlanningForbidden", Assert.Single(Category(linkedWithoutGrant, department, department.Id).Errors).Code);
        Assert.Equal("WorkPlanningForbidden", Assert.Single(Category(titled, department, department.Id).Errors).Code);
    }

    [Fact]
    public void Deactivated_manager_cannot_plan_and_administrator_can_plan_anywhere()
    {
        var department = NewDepartment();
        var manager = NewEmployee(SystemPermission.DepartmentManager);
        manager.Deactivate();

        Assert.Equal("WorkPlanningForbidden", Assert.Single(Category(manager, department, department.Id).Errors).Code);
        Assert.True(Category(NewEmployee(SystemPermission.Administrator), department).IsSuccess);
    }

    [Fact]
    public void Invalid_kind_and_closed_category_are_rejected()
    {
        var department = NewDepartment();
        var admin = NewEmployee(SystemPermission.Administrator);
        var category = Category(admin, department).Value;

        Assert.Equal("InvalidWorkItemKind", Assert.Single(_planning.CreateWork(admin, department, [], category.Id,
            TaskName.Create("Odd").Value, null, (WorkItemKind)99, false, DateTime.UtcNow).Errors).Code);
        Assert.True(department.CloseCatagory(category.Id, DateTime.UtcNow.AddMinutes(1)).IsSuccess);
        Assert.Equal("CatagoryClosed", Assert.Single(_planning.CreateWork(admin, department, [], category.Id,
            TaskName.Create("Late").Value, null, WorkItemKind.Task, false, DateTime.UtcNow).Errors).Code);
    }

    // SEC-08: work and categories cannot be moved by setting their parent.
    [Theory]
    [InlineData(typeof(WorkTask), nameof(WorkTask.CatagoryId))]
    [InlineData(typeof(Catagory), nameof(Catagory.DepartmentId))]
    public void Parent_references_have_no_public_setter(Type type, string property)
    {
        var setter = type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance)!.SetMethod;

        Assert.True(setter is null || !setter.IsPublic);
    }

    // FR-08/FR-21: assignment-only work needs an assignment even before anyone is assigned.
    [Fact]
    public void Assignment_only_work_requires_an_assignment_for_time_registration()
    {
        var department = NewDepartment();
        var admin = NewEmployee(SystemPermission.Administrator);
        var category = Category(admin, department).Value;
        var privateWork = _planning.CreateWork(admin, department, [], category.Id, TaskName.Create("Private").Value,
            null, WorkItemKind.Task, assignmentOnly: true, DateTime.UtcNow).Value;
        var generalWork = _planning.CreateWork(admin, department, [], category.Id, TaskName.Create("General").Value,
            null, WorkItemKind.Task, assignmentOnly: false, DateTime.UtcNow).Value;
        var worker = NewEmployee();

        Assert.Equal("TaskAssignmentForbidden",
            Assert.Single(Register(worker, department, privateWork.Id, hasAssignments: false, isAssigned: false).Errors).Code);
        Assert.True(Register(worker, department, privateWork.Id, hasAssignments: true, isAssigned: true).IsSuccess);
        Assert.True(Register(worker, department, generalWork.Id, hasAssignments: false, isAssigned: false).IsSuccess);
    }

    private static Result<TimeEntry> Register(Employee employee, Department department, TaskId task, bool hasAssignments, bool isAssigned)
    {
        var date = WorkDate.Create(new DateOnly(2026, 11, 14)).Value;
        return new TimeRegistrationDomainService().Register(employee, department, task, date,
            Duration.Create(2m).Value, TimeEntryDescription.Create(null).Value,
            new TimeRegistrationAccess(true, hasAssignments, isAssigned), WorkDatePeriod.Create(date, date).Value, DateTime.UtcNow);
    }
}
