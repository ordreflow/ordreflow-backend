using Application.Commands;
using Application.Handlers;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR06;

public class LoginBoundaryTests
{
    // A missing password is rejected before password verification, user lookup, or token generation.
    [Fact]
    public async Task MissingPassword_ReturnsInvalidCredentialsWithoutCallingDependencies()
    {
        var handler = new LoginHandler(new UnexpectedPasswordVerifier(),
            new MissingUserRepository(), new UnexpectedTokenGenerator());

        var result = await handler.HandleAsync(new LoginCommand("employee@example.com", ""));

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidCredentials", Assert.Single(result.Errors).Code);
    }

    // A verified identity with no corresponding domain user must not produce an access token.
    [Fact]
    public async Task VerifiedIdWithoutUser_ReturnsInvalidCredentialsWithoutToken()
    {
        var verifiedId = UserId.Create(Guid.NewGuid()).Value;
        var verifier = new ReturningPasswordVerifier(verifiedId);
        var users = new MissingUserRepository(verifiedId);
        var handler = new LoginHandler(verifier, users, new UnexpectedTokenGenerator());

        var result = await handler.HandleAsync(new LoginCommand("  EMPLOYEE@EXAMPLE.COM  ", "Correct-Password-1"));

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidCredentials", Assert.Single(result.Errors).Code);
        Assert.Equal("employee@example.com", Assert.Single(verifier.Emails).Value);
        Assert.Equal("Correct-Password-1", Assert.Single(verifier.Passwords));
        Assert.Equal(verifiedId, Assert.Single(users.RequestedIds));
    }

    private sealed class UnexpectedPasswordVerifier : IPasswordVerifier
    {
        public Task<UserId?> VerifyAsync(EmailAddress email, string password) =>
            throw new InvalidOperationException("Password verification was not expected.");
    }

    private sealed class ReturningPasswordVerifier(UserId verifiedId) : IPasswordVerifier
    {
        internal List<EmailAddress> Emails { get; } = [];
        internal List<string> Passwords { get; } = [];

        public Task<UserId?> VerifyAsync(EmailAddress email, string password)
        {
            Emails.Add(email);
            Passwords.Add(password);
            return Task.FromResult<UserId?>(verifiedId);
        }
    }

    private sealed class MissingUserRepository(UserId? expectedId = null) : IUserRepository
    {
        internal List<UserId> RequestedIds { get; } = [];

        public Task<User?> GetAsync(UserId id)
        {
            if (expectedId is null || id != expectedId)
                throw new InvalidOperationException("Unexpected user lookup.");

            RequestedIds.Add(id);
            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(User entity) => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }

    private sealed class UnexpectedTokenGenerator : IAccessTokenGenerator
    {
        public AccessToken Generate(User user) =>
            throw new InvalidOperationException("Token generation was not expected.");
    }
}
