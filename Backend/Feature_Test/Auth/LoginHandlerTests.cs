using Application.Commands;
using Application.Handlers;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Auth;

public class LoginHandlerTests
{
    private const string Password = "Correct-Password-1";

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var user = CreateUser("employee@example.com");
        var handler = CreateHandler(user);

        var result = await handler.HandleAsync(new LoginCommand("employee@example.com", Password));

        Assert.True(result.IsSuccess);
        Assert.Equal($"token-for-{user.UserId}", result.Value.AccessToken);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        var handler = CreateHandler(CreateUser("employee@example.com"));

        var result = await handler.HandleAsync(new LoginCommand("employee@example.com", "Wrong-Password-1"));

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidCredentials", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsInvalidCredentials()
    {
        var handler = CreateHandler(CreateUser("employee@example.com"));

        var result = await handler.HandleAsync(new LoginCommand("unknown@example.com", Password));

        Assert.Equal("InvalidCredentials", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsInvalidCredentials()
    {
        var handler = CreateHandler(CreateUser("employee@example.com"));

        var result = await handler.HandleAsync(new LoginCommand("not-an-email", Password));

        Assert.Equal("InvalidCredentials", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUserInactive()
    {
        var user = CreateUser("employee@example.com");
        var admin = User.Create(
            PersonName.Create("Admin").Value,
            EmailAddress.Create("admin@example.com").Value,
            UserRole.Admin).Value;
        Assert.True(user.Deactivate(admin.Role, admin.Status).IsSuccess);
        var handler = CreateHandler(user);

        var result = await handler.HandleAsync(new LoginCommand("employee@example.com", Password));

        Assert.Equal("UserInactive", Assert.Single(result.Errors).Code);
    }

    private static User CreateUser(string email) => User.Create(
        PersonName.Create("Employee").Value,
        EmailAddress.Create(email).Value).Value;

    private static LoginHandler CreateHandler(User user) => new(
        new FakePasswordVerifier(user, Password),
        new FakeUserRepository(user),
        new FakeTokenGenerator());

    private sealed class FakePasswordVerifier(User user, string password) : IPasswordVerifier
    {
        public Task<UserId?> VerifyAsync(EmailAddress email, string attempt) =>
            Task.FromResult<UserId?>(email == user.Email && attempt == password ? user.UserId : null);
    }

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetAsync(UserId id) => Task.FromResult(id == user.UserId ? user : null);

        public Task<User?> GetByEmailAsync(EmailAddress email) =>
            Task.FromResult(email == user.Email ? user : null);

        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) =>
            Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());

        public Task<IReadOnlyList<User>> GetAllAsync() =>
            Task.FromResult<IReadOnlyList<User>>(new[] { user });

        public Task AddAsync(User entity) => throw new NotSupportedException();

        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }

    private sealed class FakeTokenGenerator : IAccessTokenGenerator
    {
        public AccessToken Generate(User user) =>
            new($"token-for-{user.UserId}", DateTime.UtcNow.AddHours(1));
    }
}
