using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Features.FR14;

internal static class DepartmentTestData
{
    internal static User NewUser(string email, UserRole role, string name = "Test User") => User.Create(
        PersonName.Create(name).Value, EmailAddress.Create(email).Value, role).Value;

    internal static User Admin() => NewUser("admin@example.com", UserRole.Admin, "Alex Admin");

    internal static Department NewDepartment(string name = "Service")
    {
        var department = Department.Create(UserRole.Admin, UserStatus.Active, DepartmentName.Create(name).Value);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    internal static void AddMember(Department department, User user, bool isManager = false) =>
        Assert.True(department.SetMember(UserRole.Admin, UserStatus.Active, user.UserId, user.Role, user.Status, isManager)
            .IsSuccess);

    internal sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        internal List<User> Stored { get; } = [.. users];

        public Task<User?> GetAsync(UserId id) => Task.FromResult(Stored.SingleOrDefault(user => user.UserId == id));
        public Task<IReadOnlyList<User>> GetAllAsync() => Task.FromResult<IReadOnlyList<User>>(Stored.ToList());
        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task AddAsync(User entity) => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }

    internal sealed class FakeDepartmentRepository(params Department[] departments) : IDepartmentRepository
    {
        internal List<Department> Stored { get; } = [.. departments];

        public Task<Department?> GetAsync(DepartmentId id) =>
            Task.FromResult(Stored.SingleOrDefault(department => department.Id == id));

        public Task AddAsync(Department entity)
        {
            Stored.Add(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(DepartmentId id) => throw new NotSupportedException();

        public Task<IReadOnlyList<Department>> GetAllAsync() =>
            Task.FromResult<IReadOnlyList<Department>>(Stored.ToList());

        public Task<IReadOnlyList<Department>> GetByMemberAsync(UserId userId) =>
            Task.FromResult<IReadOnlyList<Department>>(Stored.Where(department => department.HasMember(userId)).ToList());

        public Task<bool> NameExistsAsync(DepartmentName name, DepartmentId? exceptId = null) =>
            Task.FromResult(Stored.Any(department => department.Name == name && department.Id != exceptId));
    }
}
