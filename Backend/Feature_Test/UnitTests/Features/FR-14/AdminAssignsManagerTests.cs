using Domain.ValueObjects;

namespace UnitTests.Features.FR14;

public class AdminAssignsManagerTests
{
    // An active admin can assign an active manager or admin to an employee.
    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task ActiveAdmin_AssignsManagerToEmployee(UserRole managerRole)
    {
        var scenario = new AssignManagerTestData.Scenario(managerRole: managerRole);

        var result = await scenario.Handler().HandleAsync(scenario.Command());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(scenario.Manager.UserId, scenario.Employee.ManagerId);
        Assert.Equal(new[] { scenario.Actor.UserId, scenario.Employee.UserId, scenario.Manager.UserId },
            scenario.Users.RequestedIds);
    }

    // A second valid assignment replaces the employee's existing manager relation.
    [Fact]
    public async Task ActiveAdmin_ReassignsEmployeeToDifferentManager()
    {
        var scenario = new AssignManagerTestData.Scenario();
        scenario.SeedAssignment();
        var replacement = AssignManagerTestData.NewUser("replacement@example.com", UserRole.Manager);
        scenario.Users.Stored.Add(replacement);

        var result = await scenario.Handler().HandleAsync(
            scenario.Command() with { ManagerId = replacement.UserId.Value });

        Assert.True(result.IsSuccess);
        Assert.Equal(replacement.UserId, scenario.Employee.ManagerId);
    }
}
