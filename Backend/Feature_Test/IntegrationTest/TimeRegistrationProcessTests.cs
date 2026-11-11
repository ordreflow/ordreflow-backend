using Domain.Aggregate;
using Domain.Entities;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTest.Features;

public class TimeRegistrationProcessTests
{
    [Fact]
    public void CompleteTimeRegistrationProcess_AdminCreatesUsers_ManagerAssignsWork_EmployeeSubmits_ManagerReviews()
    {
        var department = Department.Create("Elektronik").Value;
        var admin = User.Create(
            PersonName.Create("Admin Anders").Value,
            EmailAddress.Create("admin@elektronik.dk").Value,
            UserRole.Admin).Value;

        var manager = admin.CreateUser(
            PersonName.Create("Manager Mads").Value,
            EmailAddress.Create("manager@elektronik.dk").Value,
            UserRole.Manager,
            department.Id).Value;

        Assert.True(manager.CreateUser(
            PersonName.Create("Unauthorized Employee").Value,
            EmailAddress.Create("unauthorized@elektronik.dk").Value,
            UserRole.Employee,
            department.Id).IsFailure);

        var employee = admin.CreateUser(
            PersonName.Create("Peter Hansen").Value,
            EmailAddress.Create("peter@elektronik.dk").Value,
            UserRole.Employee,
            department.Id).Value;

        var order = Order.Create(
            "Reparation",
            parentOrderId: null,
            department.Id,
            department.Status,
            parentChargedDepartmentId: null,
            OrderVisibility.ChargedDepartment).Value;

        var task = order.CreateRootTask(
            manager.UserId,
            manager.Role,
            manager.Status,
            manager.DepartmentId,
            department.Id,
            department.Status,
            "Udskift printkort",
            TaskKind.Work,
            assignmentOnly: true).Value;

        var assignment = new WorkAssignmentDomainService().Assign(
            manager.UserId,
            manager.Role,
            manager.Status,
            manager.DepartmentId,
            employee.UserId,
            employee.Status,
            employee.DepartmentId,
            department.Id,
            department.Status,
            order.Id,
            order.IsVisibleIn(department.Id),
            task.Id,
            task.Kind,
            task.Status,
            expectedMinutes: 450,
            targetCompletionDate: new DateOnly(2026, 9, 10)).Value;
        Assert.True(task.AddAssignment(assignment).IsSuccess);

        var timeEntry = TimeEntry.Create(
            employee.UserId,
            task.Id,
            new DateOnly(2026, 9, 9),
            durationMinutes: 450,
            "Udskiftede og testede printkortet").Value;
        Assert.Equal(employee.UserId, timeEntry.EmployeeId);
        Assert.Equal(task.Id, timeEntry.TaskId);
        Assert.Equal(450, timeEntry.DurationMinutes);
        Assert.Equal(TimeEntryStatus.Draft, timeEntry.Status);
        Assert.Single(timeEntry.AuditEvents);

        var periodPolicy = TimeEntryPeriodPolicy.Create(
            TimeEntryPeriodMode.Open,
            monthlyCutoffDay: null,
            "Europe/Copenhagen").Value;

        var registrationResult = new TimeRegistrationDomainService().Register(
            employee.UserId,
            employee.Status,
            employee.DepartmentId,
            department.Id,
            department.Status,
            order.Id,
            order.IsVisibleIn(department.Id),
            task.Id,
            task.Status,
            task.AssignmentOnly,
            task.HasActiveAssignment(employee.UserId),
            timeEntry.Id,
            timeEntry.EmployeeId,
            timeEntry.TaskId,
            timeEntry.WorkDate,
            timeEntry.Status,
            periodPolicy,
            new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));
        Assert.True(
            registrationResult.IsSuccess,
            string.Join(Environment.NewLine, registrationResult.Errors.Select(error => $"{error.Code}: {error.Message}")));

        Assert.True(timeEntry.Submit(employee.UserId).IsSuccess);
        Assert.Equal(TimeEntryStatus.Submitted, timeEntry.Status);

        Assert.True(timeEntry.Accept(
            manager.UserId,
            manager.Role,
            manager.Status,
            manager.DepartmentId,
            employee.UserId,
            employee.DepartmentId).IsSuccess);
        Assert.Equal(TimeEntryStatus.Accepted, timeEntry.Status);
        Assert.Single(timeEntry.Reviews);
        Assert.Equal(ReviewDecision.Accepted, timeEntry.Reviews.Single().Decision);

        Assert.True(timeEntry.Finalize(
            manager.UserId,
            manager.Role,
            manager.Status,
            manager.DepartmentId,
            employee.UserId,
            employee.DepartmentId).IsSuccess);
        Assert.Equal(TimeEntryStatus.Finalized, timeEntry.Status);
        Assert.Equal(2, timeEntry.Reviews.Count);
        Assert.Equal(ReviewDecision.Finalized, timeEntry.Reviews.Last().Decision);
        Assert.Equal(4, timeEntry.AuditEvents.Count);
    }

    [Fact]
    public void ManagersAreScopedToOneDepartmentAndAdminsCanManageAllDepartments()
    {
        var firstDepartment = Department.Create("Elektronik").Value;
        var secondDepartment = Department.Create("Mekanik").Value;
        var admin = User.Create(
            PersonName.Create("Admin").Value,
            EmailAddress.Create("admin@company.test").Value,
            UserRole.Admin).Value;

        var employee = admin.CreateUser(
            PersonName.Create("Employee").Value,
            EmailAddress.Create("employee@company.test").Value,
            UserRole.Employee,
            firstDepartment.Id).Value;
        var manager = admin.CreateUser(
            PersonName.Create("Manager").Value,
            EmailAddress.Create("manager@company.test").Value,
            UserRole.Manager,
            secondDepartment.Id).Value;

        Assert.True(firstDepartment.IsManagedBy(
            admin.UserId, admin.Role, admin.Status, admin.DepartmentId));
        Assert.True(secondDepartment.IsManagedBy(
            admin.UserId, admin.Role, admin.Status, admin.DepartmentId));
        Assert.False(firstDepartment.IsManagedBy(
            manager.UserId, manager.Role, manager.Status, manager.DepartmentId));
        Assert.True(secondDepartment.IsManagedBy(
            manager.UserId, manager.Role, manager.Status, manager.DepartmentId));
        Assert.False(employee.CanView(
            manager.UserId, manager.Role, manager.Status, manager.DepartmentId));
        Assert.True(employee.CanView(
            admin.UserId, admin.Role, admin.Status, admin.DepartmentId));

        var sharedOrder = Order.Create(
            "Shared order",
            parentOrderId: null,
            firstDepartment.Id,
            firstDepartment.Status,
            parentChargedDepartmentId: null,
            OrderVisibility.AllDepartments).Value;
        var task = sharedOrder.CreateRootTask(
            admin.UserId,
            admin.Role,
            admin.Status,
            admin.DepartmentId,
            firstDepartment.Id,
            firstDepartment.Status,
            "Shared task",
            TaskKind.Work,
            assignmentOnly: false).Value;
        var entry = TimeEntry.Create(
            employee.UserId,
            task.Id,
            new DateOnly(2026, 9, 9),
            durationMinutes: 60,
            comment: null).Value;

        Assert.True(entry.Submit(employee.UserId).IsSuccess);
        Assert.True(entry.Accept(
            manager.UserId,
            manager.Role,
            manager.Status,
            manager.DepartmentId,
            employee.UserId,
            employee.DepartmentId).IsFailure);
        Assert.True(entry.Accept(
            admin.UserId,
            admin.Role,
            admin.Status,
            admin.DepartmentId,
            employee.UserId,
            employee.DepartmentId).IsSuccess);
    }

    [Fact]
    public void TaskVisibilityIsDepartmentWideWhenUnassignedAndRestrictedToAssigneesWhenAssigned()
    {
        var department = Department.Create("Elektronik").Value;
        var otherDepartment = Department.Create("Mekanik").Value;
        var admin = User.Create(
            PersonName.Create("Admin").Value,
            EmailAddress.Create("admin@visibility.test").Value,
            UserRole.Admin).Value;

        var manager = admin.CreateUser(
            PersonName.Create("Manager").Value,
            EmailAddress.Create("manager@visibility.test").Value,
            UserRole.Manager,
            department.Id).Value;
        var assignedEmployee = admin.CreateUser(
            PersonName.Create("Assigned").Value,
            EmailAddress.Create("assigned@visibility.test").Value,
            UserRole.Employee,
            department.Id).Value;
        var colleague = admin.CreateUser(
            PersonName.Create("Colleague").Value,
            EmailAddress.Create("colleague@visibility.test").Value,
            UserRole.Employee,
            department.Id).Value;
        var outsider = admin.CreateUser(
            PersonName.Create("Outsider").Value,
            EmailAddress.Create("outsider@visibility.test").Value,
            UserRole.Employee,
            otherDepartment.Id).Value;

        var order = Order.Create(
            "Shared order",
            parentOrderId: null,
            department.Id,
            department.Status,
            parentChargedDepartmentId: null,
            OrderVisibility.ChargedDepartment).Value;
        var assignedTask = order.CreateRootTask(
            manager.UserId,
            manager.Role,
            manager.Status,
            manager.DepartmentId,
            department.Id,
            department.Status,
            "Assigned task",
            TaskKind.Work,
            assignmentOnly: true).Value;
        var openTask = order.CreateRootTask(
            manager.UserId,
            manager.Role,
            manager.Status,
            manager.DepartmentId,
            department.Id,
            department.Status,
            "Open task",
            TaskKind.Work,
            assignmentOnly: false).Value;

        var assignment = WorkAssignment.Create(
            assignedTask.Id,
            assignedEmployee.UserId,
            expectedMinutes: 60,
            targetCompletionDate: null).Value;
        Assert.True(assignedTask.AddAssignment(assignment).IsSuccess);

        Assert.True(assignedTask.CanView(
            assignedEmployee.UserId, assignedEmployee.Role, assignedEmployee.Status,
            assignedEmployee.DepartmentId, department.Id, department.Status,
            order.Id, order.IsVisibleIn(department.Id)));
        Assert.False(assignedTask.CanView(
            colleague.UserId, colleague.Role, colleague.Status,
            colleague.DepartmentId, department.Id, department.Status,
            order.Id, order.IsVisibleIn(department.Id)));
        Assert.True(assignedTask.CanView(
            manager.UserId, manager.Role, manager.Status,
            manager.DepartmentId, department.Id, department.Status,
            order.Id, order.IsVisibleIn(department.Id)));
        Assert.True(assignedTask.CanView(
            admin.UserId, admin.Role, admin.Status,
            admin.DepartmentId, otherDepartment.Id, otherDepartment.Status,
            order.Id, order.IsVisibleIn(otherDepartment.Id)));

        Assert.True(openTask.CanView(
            assignedEmployee.UserId, assignedEmployee.Role, assignedEmployee.Status,
            assignedEmployee.DepartmentId, department.Id, department.Status,
            order.Id, order.IsVisibleIn(department.Id)));
        Assert.True(openTask.CanView(
            colleague.UserId, colleague.Role, colleague.Status,
            colleague.DepartmentId, department.Id, department.Status,
            order.Id, order.IsVisibleIn(department.Id)));
        Assert.False(openTask.CanView(
            outsider.UserId, outsider.Role, outsider.Status,
            outsider.DepartmentId, otherDepartment.Id, otherDepartment.Status,
            order.Id, order.IsVisibleIn(otherDepartment.Id)));
    }
}
