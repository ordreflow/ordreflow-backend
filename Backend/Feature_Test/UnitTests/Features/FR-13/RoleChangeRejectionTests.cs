using Domain.ValueObjects;

namespace UnitTests.Features.FR13;

public class RoleChangeRejectionTests
{
    // Invalid text is rejected before user lookup, without changing the target's role.
    [Fact]
    public async Task UnknownRole_IsRejectedBeforeRepositoryAccess()
    {
        var scenario = new RoleChangeTestData.Scenario();
        var originalRole = scenario.Target.Role;

        var result = await scenario.Handler().HandleAsync(scenario.Command("Wizard"));

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidRole", Assert.Single(result.Errors).Code);
        Assert.Equal(originalRole, scenario.Target.Role);
        Assert.Empty(scenario.Users.RequestedIds);
    }

    // Enum parsing accepts numeric text, but the domain rejects values outside its supported roles.
    [Fact]
    public async Task UndefinedNumericRole_IsRejectedWithoutMutation()
    {
        var scenario = new RoleChangeTestData.Scenario();
        var originalRole = scenario.Target.Role;

        var result = await scenario.Handler().HandleAsync(scenario.Command("999"));

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidRole", Assert.Single(result.Errors).Code);
        Assert.Equal(originalRole, scenario.Target.Role);
        Assert.Equal(new[] { scenario.Actor.UserId, scenario.Target.UserId }, scenario.Users.RequestedIds);
    }

    // Employees, managers, and inactive admins cannot assign roles, even to an existing target.
    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, false)]
    [InlineData(UserRole.Admin, true)]
    public async Task UnauthorizedOrInactiveActor_DoesNotChangeTargetRole(UserRole actorRole, bool inactive)
    {
        var scenario = new RoleChangeTestData.Scenario(actorRole);
        if (inactive)
            Assert.True(scenario.Actor.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        var originalRole = scenario.Target.Role;

        var result = await scenario.Handler().HandleAsync(scenario.Command());

        Assert.True(result.IsFailure);
        Assert.Equal("RoleChangeForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(originalRole, scenario.Target.Role);
        Assert.Equal(new[] { scenario.Actor.UserId, scenario.Target.UserId }, scenario.Users.RequestedIds);
    }

    // Either missing user produces UserNotFound without mutating the target.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingActorOrTarget_DoesNotChangeRole(bool missingActor)
    {
        var scenario = new RoleChangeTestData.Scenario();
        var originalRole = scenario.Target.Role;
        scenario.Users.Stored.Remove(missingActor ? scenario.Actor : scenario.Target);

        var result = await scenario.Handler().HandleAsync(scenario.Command());

        Assert.True(result.IsFailure);
        Assert.Equal("UserNotFound", Assert.Single(result.Errors).Code);
        Assert.Equal(originalRole, scenario.Target.Role);
    }

    // Empty actor or target IDs fail validation before any repository call.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptyActorOrTargetId_DoesNotLookUpOrChangeUser(bool emptyActorId)
    {
        var scenario = new RoleChangeTestData.Scenario();
        var originalRole = scenario.Target.Role;
        var command = emptyActorId
            ? scenario.Command() with { ActorId = Guid.Empty }
            : scenario.Command() with { UserId = Guid.Empty };

        var result = await scenario.Handler().HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("UserIdEmpty", Assert.Single(result.Errors).Code);
        Assert.Equal(originalRole, scenario.Target.Role);
        Assert.Empty(scenario.Users.RequestedIds);
    }
}
