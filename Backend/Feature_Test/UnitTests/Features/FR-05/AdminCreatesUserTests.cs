using Domain.ValueObjects;

namespace UnitTests.Features.FR05;

public class AdminCreatesUserTests
{
    // An active admin can create each supported role; the domain user is added before its account is provisioned.
    [Theory]
    [InlineData("Employee", UserRole.Employee)]
    [InlineData("mAnAgEr", UserRole.Manager)]
    [InlineData("Admin", UserRole.Admin)]
    public async Task ActiveAdmin_CreatesUserAndProvisionsMatchingAccount(string role, UserRole expectedRole)
    {
        var scenario = new CreateUserTestData.Scenario();
        scenario.AllowCreation();

        var result = await scenario.Handler().HandleAsync(scenario.Command(role));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        var saved = Assert.Single(scenario.Users.Added);
        Assert.Equal("New User", saved.Name.Value);
        Assert.Equal("new.user@example.com", saved.Email.Value);
        Assert.Equal(expectedRole, saved.Role);
        Assert.Equal(UserStatus.Active, saved.Status);
        Assert.Null(saved.ManagerId);
        var account = Assert.Single(scenario.Accounts.Created);
        Assert.Equal(saved.UserId, account.UserId);
        Assert.Equal(saved.Email, account.Email);
        Assert.Equal(CreateUserTestData.Password, account.Password);
        Assert.Equal(expectedRole, account.Role);
        Assert.Equal(new[]
        {
            $"Get:{scenario.Actor.UserId}", $"Add:{saved.UserId}", $"Provision:{saved.UserId}"
        }, scenario.Events);
    }

    // An optional active manager or admin is assigned to the new user before it is added and provisioned.
    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task ActiveAdmin_CreatesEmployeeWithManager(UserRole managerRole)
    {
        var scenario = new CreateUserTestData.Scenario();
        var manager = CreateUserTestData.NewUser("manager@example.com", managerRole);
        scenario.Users.Stored.Add(manager);
        scenario.AllowCreation();

        var result = await scenario.Handler().HandleAsync(scenario.Command(managerId: manager.UserId.Value));

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(scenario.Users.Added);
        Assert.Equal(manager.UserId, saved.ManagerId);
        Assert.Equal(UserRole.Employee, saved.Role);
        Assert.Equal(saved.UserId, Assert.Single(scenario.Accounts.Created).UserId);
        Assert.Equal(new[]
        {
            $"Get:{scenario.Actor.UserId}", $"Get:{manager.UserId}",
            $"Add:{saved.UserId}", $"Provision:{saved.UserId}"
        }, scenario.Events);
    }
}
