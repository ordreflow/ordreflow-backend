using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace UnitTests.Features.FR05;

public class CreateUserRejectionTests
{
    // Invalid input is rejected before looking up an actor, adding a user, or provisioning an account.
    [Theory]
    [InlineData("actor", "UserIdEmpty")]
    [InlineData("name", "PersonNameEmpty")]
    [InlineData("email", "EmailInvalid")]
    [InlineData("manager", "UserIdEmpty")]
    [InlineData("password", "PasswordRequired")]
    [InlineData("role", "InvalidRole")]
    public async Task InvalidCommand_DoesNotReachExternalDependencies(string field, string errorCode)
    {
        var scenario = new CreateUserTestData.Scenario();
        var command = scenario.Command();
        command = field switch
        {
            "actor" => command with { ActorId = Guid.Empty },
            "name" => command with { Name = "  " },
            "email" => command with { Email = "not-an-email" },
            "manager" => command with { ManagerId = Guid.Empty },
            "password" => command with { Password = "  " },
            "role" => command with { Role = "Wizard" },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        AssertFailure(errorCode, await scenario.Handler().HandleAsync(command), scenario);

        Assert.Empty(scenario.Events);
    }

    // A valid but unknown actor ID cannot create a domain user or a login account.
    [Fact]
    public async Task MissingActor_DoesNotCreateUser()
    {
        var scenario = new CreateUserTestData.Scenario();
        scenario.Users.Stored.Clear();

        AssertFailure("UserNotFound", await scenario.Handler().HandleAsync(scenario.Command()), scenario);

        Assert.Equal($"Get:{scenario.Actor.UserId}", Assert.Single(scenario.Events));
    }

    // An employee or inactive admin is not allowed to create users under the current domain rule.
    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Admin, true)]
    public async Task ActorWithoutCreationPermission_DoesNotCreateUser(UserRole actorRole, bool inactive)
    {
        var scenario = new CreateUserTestData.Scenario(actorRole);
        if (inactive)
            Assert.True(scenario.Actor.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        AssertFailure("UserCreationForbidden", await scenario.Handler().HandleAsync(scenario.Command()), scenario);

        Assert.Equal($"Get:{scenario.Actor.UserId}", Assert.Single(scenario.Events));
    }

    // A requested manager must exist before either domain-user add or account creation.
    [Fact]
    public async Task UnknownManager_DoesNotCreateUser()
    {
        var scenario = new CreateUserTestData.Scenario();
        var missingId = Guid.NewGuid();

        AssertFailure("ManagerNotFound",
            await scenario.Handler().HandleAsync(scenario.Command(managerId: missingId)), scenario);

        Assert.Equal(new[] { $"Get:{scenario.Actor.UserId}", $"Get:{missingId}" }, scenario.Events);
    }

    // An employee or inactive manager is not a valid assignment candidate during creation.
    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, true)]
    public async Task InvalidManager_DoesNotCreateUser(UserRole managerRole, bool inactive)
    {
        var scenario = new CreateUserTestData.Scenario();
        var manager = CreateUserTestData.NewUser("manager@example.com", managerRole);
        if (inactive)
            Assert.True(manager.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        scenario.Users.Stored.Add(manager);

        AssertFailure("InvalidManager",
            await scenario.Handler().HandleAsync(scenario.Command(managerId: manager.UserId.Value)), scenario);

        Assert.Equal(new[] { $"Get:{scenario.Actor.UserId}", $"Get:{manager.UserId}" }, scenario.Events);
    }

    // Provisioning errors are returned after the already-added domain user; this handler does not undo the add.
    [Fact]
    public async Task ProvisioningFailure_ReturnsErrorAfterUserAdd()
    {
        var scenario = new CreateUserTestData.Scenario();
        scenario.AllowCreation();
        scenario.Accounts.Response = Result.Failure(new Error("AccountCreationFailed", "Account not created."));

        var result = await scenario.Handler().HandleAsync(scenario.Command());

        Assert.True(result.IsFailure);
        Assert.Equal("AccountCreationFailed", Assert.Single(result.Errors).Code);
        var added = Assert.Single(scenario.Users.Added);
        Assert.Equal(added.UserId, Assert.Single(scenario.Accounts.Created).UserId);
        Assert.Equal(new[]
        {
            $"Get:{scenario.Actor.UserId}", $"Add:{added.UserId}", $"Provision:{added.UserId}"
        }, scenario.Events);
    }

    private static void AssertFailure(string code, Result result, CreateUserTestData.Scenario scenario)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
        Assert.Empty(scenario.Users.Added);
        Assert.Empty(scenario.Accounts.Created);
    }
}
