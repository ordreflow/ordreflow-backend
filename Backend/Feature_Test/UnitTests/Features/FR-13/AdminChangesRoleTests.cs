using Domain.ValueObjects;

namespace UnitTests.Features.FR13;

public class AdminChangesRoleTests
{
    // An active admin can assign the supported roles, including case-insensitive role names.
    [Theory]
    [InlineData(UserRole.Employee, "Manager", UserRole.Manager)]
    [InlineData(UserRole.Employee, "Admin", UserRole.Admin)]
    [InlineData(UserRole.Manager, "eMpLoYeE", UserRole.Employee)]
    public async Task ActiveAdmin_ChangesOnlyTargetRole(UserRole initialRole, string role, UserRole expectedRole)
    {
        var scenario = new RoleChangeTestData.Scenario(targetRole: initialRole);
        var originalStatus = scenario.Target.Status;

        var result = await scenario.Handler().HandleAsync(scenario.Command(role));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(expectedRole, scenario.Target.Role);
        Assert.Equal(originalStatus, scenario.Target.Status);
        Assert.Equal(UserRole.Admin, scenario.Actor.Role);
        Assert.Equal(new[] { scenario.Actor.UserId, scenario.Target.UserId }, scenario.Users.RequestedIds);
    }
}
