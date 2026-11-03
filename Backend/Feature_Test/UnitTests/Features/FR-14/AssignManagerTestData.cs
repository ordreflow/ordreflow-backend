using Application.Commands;
using Application.Handlers;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR14;

internal static class AssignManagerTestData
{
    internal static User NewUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    internal sealed class Scenario
    {
        internal User Actor { get; }
        internal User Employee { get; }
        internal User Manager { get; }
        internal FakeUserRepository Users { get; }

        internal Scenario(UserRole actorRole = UserRole.Admin, UserRole managerRole = UserRole.Manager)
        {
            Actor = NewUser("admin@example.com", actorRole);
            Employee = NewUser("employee@example.com", UserRole.Employee);
            Manager = NewUser("manager@example.com", managerRole);
            Users = new FakeUserRepository(Actor, Employee, Manager);
        }

        internal AssignManagerCommand Command() =>
            new(Actor.UserId.Value, Employee.UserId.Value, Manager.UserId.Value);

        internal AssignManagerHandler Handler() => new(Users);

        internal void SeedAssignment()
        {
            var result = Employee.AssignManager(Manager.UserId, Manager.Role, Manager.Status,
                UserRole.Admin, UserStatus.Active);
            if (result.IsFailure)
                throw new InvalidOperationException("Could not seed the employee's manager.");
        }
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
