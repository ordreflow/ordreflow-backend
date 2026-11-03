using Domain.Aggregate;
using Domain.ValueObjects;

namespace UnitTests.Features.FR01;

internal static class TimeRegistrationTestData
{
    internal static (User Manager, User Employee, Order Order, Domain.Entities.Task Task) CreateWork(
        string employeeEmail = "employee@example.com", string taskTitle = "Repair")
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var employee = CreateUser(employeeEmail, UserRole.Employee);
        var order = CreateOrder(manager);
        var task = AddTask(order, manager, taskTitle);
        return (manager, employee, order, task);
    }

    internal static (User Manager, User Employee, Order Order, Domain.Entities.Task Task) CreateAssignedWork(
        string employeeEmail = "employee@example.com", string taskTitle = "Repair")
    {
        var (manager, employee, order, task) = CreateWork(employeeEmail, taskTitle);
        AssignToManager(employee, manager);
        return (manager, employee, order, task);
    }

    internal static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    internal static User CreateAssignedEmployee(User manager, string email) =>
        AssignToManager(CreateUser(email, UserRole.Employee), manager);

    internal static Order CreateOrder(User manager) => Order.Create(
        manager.UserId, manager.Role, manager.Status, OrderName.Create("Repair order").Value).Value;

    internal static Domain.Entities.Task AddTask(Order order, User manager, string title) =>
        order.AddWorkItem(manager.UserId, manager.Role, manager.Status, title, "Repair description").Value;

    internal static TimeEntry CreateEntry(User employee, TaskId taskId) => TimeEntry.Create(
        employee.UserId, taskId, new DateTime(2026, 9, 9), 2m, "Repair").Value;

    private static User AssignToManager(User employee, User manager)
    {
        var result = employee.AssignManager(manager.UserId, manager.Role, manager.Status,
            UserRole.Admin, UserStatus.Active);
        if (result.IsFailure)
            throw new InvalidOperationException("Could not assign the test employee to the manager.");
        return employee;
    }
}
