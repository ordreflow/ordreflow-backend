using Domain.ValueObjects;

namespace UnitTests.Features.FR14;

public class AssignManagerRejectionTests
{
    // Invalid IDs fail before fetching users and cannot replace an existing manager relation.
    [Theory]
    [InlineData("admin")]
    [InlineData("employee")]
    [InlineData("manager")]
    public async Task EmptyId_DoesNotLookUpOrReassign(string field)
    {
        var scenario = new AssignManagerTestData.Scenario();
        scenario.SeedAssignment();
        var command = field switch
        {
            "admin" => scenario.Command() with { AdminId = Guid.Empty },
            "employee" => scenario.Command() with { EmployeeId = Guid.Empty },
            "manager" => scenario.Command() with { ManagerId = Guid.Empty },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        var result = await scenario.Handler().HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("UserIdEmpty", Assert.Single(result.Errors).Code);
        Assert.Equal(scenario.Manager.UserId, scenario.Employee.ManagerId);
        Assert.Empty(scenario.Users.RequestedIds);
    }

    // Missing actors, employees, or a new manager candidate leave the existing assignment unchanged.
    [Theory]
    [InlineData("admin")]
    [InlineData("employee")]
    [InlineData("manager")]
    public async Task MissingUser_DoesNotReassign(string missing)
    {
        var scenario = new AssignManagerTestData.Scenario();
        scenario.SeedAssignment();
        var command = scenario.Command();
        if (missing == "manager")
            command = command with { ManagerId = Guid.NewGuid() };
        else
            scenario.Users.Stored.Remove(missing switch
            {
                "admin" => scenario.Actor,
                "employee" => scenario.Employee,
                _ => throw new ArgumentOutOfRangeException(nameof(missing))
            });

        var result = await scenario.Handler().HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("UserNotFound", Assert.Single(result.Errors).Code);
        Assert.Equal(scenario.Manager.UserId, scenario.Employee.ManagerId);
    }

    // An employee or manager actor, or an inactive admin, cannot change an existing assignment.
    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, false)]
    [InlineData(UserRole.Admin, true)]
    public async Task UnauthorizedOrInactiveActor_DoesNotReassign(UserRole actorRole, bool inactive)
    {
        var scenario = new AssignManagerTestData.Scenario(actorRole);
        scenario.SeedAssignment();
        if (inactive)
            Assert.True(scenario.Actor.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        var result = await scenario.Handler().HandleAsync(scenario.Command());

        Assert.True(result.IsFailure);
        Assert.Equal("ManagerAssignmentForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(scenario.Manager.UserId, scenario.Employee.ManagerId);
    }

    // A user cannot be its own manager, even when it already has a valid manager.
    [Fact]
    public async Task SelfAssignment_DoesNotReplaceExistingManager()
    {
        var scenario = new AssignManagerTestData.Scenario();
        scenario.SeedAssignment();

        var result = await scenario.Handler().HandleAsync(
            scenario.Command() with { ManagerId = scenario.Employee.UserId.Value });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidManager", Assert.Single(result.Errors).Code);
        Assert.Equal(scenario.Manager.UserId, scenario.Employee.ManagerId);
    }

    // An active employee or inactive manager cannot replace the employee's existing manager.
    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, true)]
    public async Task InvalidManagerCandidate_DoesNotReplaceExistingManager(UserRole candidateRole, bool inactive)
    {
        var scenario = new AssignManagerTestData.Scenario();
        scenario.SeedAssignment();
        var candidate = AssignManagerTestData.NewUser("candidate@example.com", candidateRole);
        if (inactive)
            Assert.True(candidate.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        scenario.Users.Stored.Add(candidate);

        var result = await scenario.Handler().HandleAsync(
            scenario.Command() with { ManagerId = candidate.UserId.Value });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidManager", Assert.Single(result.Errors).Code);
        Assert.Equal(scenario.Manager.UserId, scenario.Employee.ManagerId);
    }
}
