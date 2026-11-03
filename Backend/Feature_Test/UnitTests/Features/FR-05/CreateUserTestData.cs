using Application.Commands;
using Application.Handlers;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR05;

internal static class CreateUserTestData
{
    internal const string Password = "Str0ngPass!";

    internal static User NewUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    internal sealed class Scenario
    {
        internal User Actor { get; }
        internal List<string> Events { get; } = [];
        internal FakeUserRepository Users { get; }
        internal FakeAccountProvisioner Accounts { get; }

        internal Scenario(UserRole actorRole = UserRole.Admin)
        {
            Actor = NewUser("admin@example.com", actorRole);
            Users = new FakeUserRepository(Events, Actor);
            Accounts = new FakeAccountProvisioner(Events);
        }

        internal CreateUserCommand Command(string role = "Employee", Guid? managerId = null) =>
            new(Actor.UserId.Value, "  New User  ", "  NEW.USER@Example.com  ", Password, role, managerId);

        internal CreateUserHandler Handler() => new(Users, Accounts);

        internal void AllowCreation()
        {
            Users.AllowAdd = true;
            Accounts.AllowCreate = true;
        }
    }

    internal sealed class FakeUserRepository(List<string> events, params User[] users) : IUserRepository
    {
        internal List<User> Stored { get; } = [.. users];
        internal List<User> Added { get; } = [];
        internal bool AllowAdd { get; set; }

        public Task<User?> GetAsync(UserId id)
        {
            events.Add($"Get:{id}");
            return Task.FromResult(Stored.SingleOrDefault(user => user.UserId == id));
        }

        public Task AddAsync(User entity)
        {
            if (!AllowAdd)
                throw new InvalidOperationException("Unexpected domain user add.");

            events.Add($"Add:{entity.UserId}");
            Added.Add(entity);
            Stored.Add(entity);
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }

    internal sealed class FakeAccountProvisioner(List<string> events) : IUserAccountProvisioner
    {
        internal bool AllowCreate { get; set; }
        internal Result Response { get; set; } = Result.Success();
        internal List<ProvisionCall> Created { get; } = [];

        public Task<Result> CreateAsync(UserId userId, EmailAddress email, string password, UserRole role)
        {
            if (!AllowCreate)
                throw new InvalidOperationException("Unexpected account provisioning.");

            events.Add($"Provision:{userId}");
            Created.Add(new ProvisionCall(userId, email, password, role));
            return Task.FromResult(Response);
        }
    }

    internal sealed record ProvisionCall(UserId UserId, EmailAddress Email, string Password, UserRole Role);
}
