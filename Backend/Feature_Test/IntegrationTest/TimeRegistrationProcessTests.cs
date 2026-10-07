using Domain.Aggregate;
using Domain.Entities;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTest.Features;

public class TimeRegistrationProcessTests
{
    [Fact]
    public void CompleteTimeRegistrationProcess_ManagerCreatesEmployeeOrderTask_EmployeeRegisters_ManagerApprovesAndFinalizes()
    {
      // ---------------------------------------------------------
// 1. Create admin
// ---------------------------------------------------------

var adminName = PersonName.Create("Admin Anders").Value;
var adminEmail = EmailAddress.Create("admin@elektronik.dk").Value;

var adminResult = User.Create(
    adminName,
    adminEmail,
    UserRole.Admin);

Assert.True(adminResult.IsSuccess);

var admin = adminResult.Value;

Console.WriteLine(
    $"1. Admin created: {admin.Name.Value} ({admin.UserId})");


// ---------------------------------------------------------
// 2. Create manager
// ---------------------------------------------------------

var managerName = PersonName.Create("Manager Mads").Value;
var managerEmail = EmailAddress.Create("manager@elektronik.dk").Value;

var managerResult = User.Create(
    managerName,
    managerEmail,
    UserRole.Manager);

Assert.True(managerResult.IsSuccess);

var manager = managerResult.Value;

Console.WriteLine(
    $"2. Manager created: {manager.Name.Value} ({manager.UserId})");


// ---------------------------------------------------------
// 3. Manager creates employee
// ---------------------------------------------------------

var employeeName = PersonName.Create("Peter Hansen").Value;
var employeeEmail = EmailAddress.Create("peter@elektronik.dk").Value;

var employeeResult = manager.CreateUser(
    employeeName,
    employeeEmail,
    UserRole.Employee);

Assert.True(employeeResult.IsSuccess);

var employee = employeeResult.Value;

Console.WriteLine(
    $"3. Employee created: {employee.Name.Value} ({employee.UserId})");


// ---------------------------------------------------------
// 4. Admin assigns employee to manager
// ---------------------------------------------------------

var assignManagerResult = employee.AssignManager(
    manager.UserId,
    admin.Role,
    admin.Status);

Assert.True(
    assignManagerResult.IsSuccess,
    string.Join(
        Environment.NewLine,
        assignManagerResult.Errors.Select(
            error => $"{error.Code}: {error.Message}")));

Assert.Equal(manager.UserId, employee.ManagerId);

Console.WriteLine(
    $"4. Employee assigned to manager: {manager.Name.Value}");

        // ---------------------------------------------------------
        // 4. Manager creates OrderCommands
        // ---------------------------------------------------------

        var orderName = OrderName.Create(
            "Reparation af styringsenhed").Value;

        var orderResult = Order.Create(
            manager.UserId,
            manager.Role,
            manager.Status,
            orderName);

        Assert.True(orderResult.IsSuccess);

        var order = orderResult.Value;

        Assert.Equal(manager.UserId, order.ManagerId);
        Assert.Equal(OrderStatus.Open, order.Status);

        Console.WriteLine(
            $"3. OrderCommands created: {order.Name.Value} ({order.Id})");


        // ---------------------------------------------------------
        // 5. Manager creates Task on the OrderCommands
        // ---------------------------------------------------------

        var taskResult = order.AddWorkItem(
            manager.UserId,
            manager.Role,
            manager.Status,
            "Udskift printkort",
            "Udskift og test defekt printkort.");

        Assert.True(taskResult.IsSuccess);

        var task = taskResult.Value;

        Assert.Equal(order.Id, task.OrderId);
        Assert.True(order.ContainsWorkItem(task.TaskId));

        Console.WriteLine(
            $"4. Task created: {task.Title} ({task.TaskId})");


        // ---------------------------------------------------------
        // 6. Create TimeEntry
        // ---------------------------------------------------------
        //
        // TimeEntry.Create() creates the actual domain object.
        // It does NOT save anything to the database.
        // ---------------------------------------------------------

        var timeEntryResult = TimeEntry.Create(
            employee.UserId,
            task.TaskId,
            new DateTime(2026, 9, 9),
            7.5m,
            "Udskiftede og testede printkortet");

        Assert.True(timeEntryResult.IsSuccess);

        var timeEntry = timeEntryResult.Value;

        Assert.Equal(employee.UserId, timeEntry.EmployeeId);
        Assert.Equal(task.TaskId, timeEntry.TaskId);
        Assert.Equal(7.5m, timeEntry.Hours);
        Assert.Equal(TimeEntryStatus.Draft, timeEntry.Status);

        Console.WriteLine(
            $"5. TimeEntry created: {timeEntry.Hours} hours on task '{task.Title}'");


        // ---------------------------------------------------------
        // 7. Validate time registration through Domain Service
        // ---------------------------------------------------------
        //
        // The Domain Service coordinates the rules between:
        // User + OrderCommands + TimeEntry.
        //
        // OrderCommands.CanRegisterTime() verifies:
        // - OrderCommands is open
        // - Task belongs to OrderCommands
        // - Employee belongs to the manager scope
        // ---------------------------------------------------------

        var registrationService = new TimeRegistrationDomainService();

        var registrationResult = registrationService.Register(
            employee,
            order,
            timeEntry);

        Assert.True(registrationResult.IsSuccess);
        Assert.Equal(TimeEntryStatus.Draft, timeEntry.Status);

        Console.WriteLine(
            $"6. Employee registered {timeEntry.Hours} hours successfully");


        // ---------------------------------------------------------
        // 8. Manager accepts TimeEntry
        // ---------------------------------------------------------

        var acceptResult = timeEntry.Accept(
            manager.UserId,
            employee.UserId,
            employee.ManagerId,
            manager.Role,
            manager.Status);

        Assert.True(acceptResult.IsSuccess);
        Assert.Equal(TimeEntryStatus.Accepted, timeEntry.Status);

        Assert.Single(timeEntry.Reviews);
        Assert.Equal(
            ReviewDecision.Accepted,
            timeEntry.Reviews.First().Decision);

        Console.WriteLine(
            "7. Manager accepted the time entry");


        // ---------------------------------------------------------
        // 9. Manager finalizes TimeEntry
        // ---------------------------------------------------------

        var finalizeResult = timeEntry.Finalize(
            manager.UserId,
            employee.UserId,
            employee.ManagerId,
            manager.Role,
            manager.Status);

        Assert.True(finalizeResult.IsSuccess);
        Assert.Equal(TimeEntryStatus.Finalized, timeEntry.Status);

        Assert.Equal(2, timeEntry.Reviews.Count);

        Assert.Equal(
            ReviewDecision.Finalized,
            timeEntry.Reviews.Last().Decision);

        Console.WriteLine(
            "8. Manager finalized the time entry");


        // ---------------------------------------------------------
        // 10. Final assertions
        // ---------------------------------------------------------

        Assert.Equal(employee.UserId, timeEntry.EmployeeId);
        Assert.Equal(task.TaskId, timeEntry.TaskId);
        Assert.Equal(order.Id, task.OrderId);
        Assert.Equal(manager.UserId, order.ManagerId);

        Console.WriteLine("=== Process completed successfully ===");
    }
}

