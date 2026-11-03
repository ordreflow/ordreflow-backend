using Application.Commands;
using Application.Handlers;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR13;

internal static class RoleChangeTestData
{
    internal static User NewUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    internal sealed class Scenario
    {
        internal User Actor { get; }
        internal User Target { get; }
        internal FakeUserRepository Users { get; }

        internal Scenario(UserRole actorRole = UserRole.Admin, UserRole targetRole = UserRole.Employee)
        {
            Actor = NewUser("admin@example.com", actorRole);
            Target = NewUser("target@example.com", targetRole);
            Users = new FakeUserRepository(Actor, Target);
        }

        internal ChangeUserRoleCommand Command(string role = "Manager") =>
            new(Actor.UserId.Value, Target.UserId.Value, role);

        internal ChangeUserRoleHandler Handler() => new(Users);
    }

    internal sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        internal List<User> Stored { get; } = [.. users];
        internal List<UserId> RequestedIds { get; } = [];

        public Task<User?> GetAsync(UserId id)
        {
            RequestedIds.Add(id);
            return Task.FromResult(Stored.SingleOrDefault(user => user.UserId == id));
        }

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(User entity) => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }
}
