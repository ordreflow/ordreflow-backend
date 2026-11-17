using Application.Commands;
using Application.Dtos;
using Application.Handlers;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;
using static UnitTests.Features.FR14.DepartmentTestData;

namespace UnitTests.Features.FR14;

public class DepartmentCommandHandlerTests
{
    [Fact]
    public async Task Admin_CreatesDepartment()
    {
        var admin = Admin();
        var departments = new FakeDepartmentRepository();
        var handler = new CreateDepartmentHandler(new FakeUserRepository(admin), departments);

        var result = await handler.HandleAsync(new CreateDepartmentCommand(admin.UserId.Value, " Service "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Service", Assert.Single(departments.Stored).Name.Value);
    }

    [Fact]
    public async Task DuplicateDepartmentName_IsRejected()
    {
        var admin = Admin();
        var departments = new FakeDepartmentRepository(NewDepartment("Service"));
        var handler = new CreateDepartmentHandler(new FakeUserRepository(admin), departments);

        var result = await handler.HandleAsync(new CreateDepartmentCommand(admin.UserId.Value, "Service"));

        Assert.Equal("DepartmentNameTaken", Assert.Single(result.Errors).Code);
        Assert.Single(departments.Stored);
    }

    [Fact]
    public async Task Manager_CannotCreateDepartment()
    {
        var manager = NewUser("manager@example.com", UserRole.Manager);
        var departments = new FakeDepartmentRepository();
        var handler = new CreateDepartmentHandler(new FakeUserRepository(manager), departments);

        var result = await handler.HandleAsync(new CreateDepartmentCommand(manager.UserId.Value, "Service"));

        Assert.Equal("DepartmentManagementForbidden", Assert.Single(result.Errors).Code);
        Assert.Empty(departments.Stored);
    }

    [Fact]
    public async Task Rename_ToOtherDepartmentsName_IsRejected_ButKeepingOwnNameIsAllowed()
    {
        var admin = Admin();
        var service = NewDepartment("Service");
        var sales = NewDepartment("Sales");
        var handler = new RenameDepartmentHandler(
            new FakeUserRepository(admin), new FakeDepartmentRepository(service, sales));

        var taken = await handler.HandleAsync(new RenameDepartmentCommand(admin.UserId.Value, service.Id.Value, "Sales"));
        var same = await handler.HandleAsync(new RenameDepartmentCommand(admin.UserId.Value, sales.Id.Value, "Sales"));

        Assert.Equal("DepartmentNameTaken", Assert.Single(taken.Errors).Code);
        Assert.True(same.IsSuccess);
    }

    [Fact]
    public async Task Rename_UnknownDepartment_IsNotFound()
    {
        var admin = Admin();
        var handler = new RenameDepartmentHandler(new FakeUserRepository(admin), new FakeDepartmentRepository());

        var result = await handler.HandleAsync(new RenameDepartmentCommand(admin.UserId.Value, Guid.NewGuid(), "Sales"));

        Assert.Equal("DepartmentNotFound", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task SetMember_UsesTheStoredUsersAccess_NotTheRequest()
    {
        var admin = Admin();
        var employee = NewUser("employee@example.com", UserRole.Employee);
        var department = NewDepartment();
        var handler = new SetDepartmentMemberHandler(
            new FakeUserRepository(admin, employee), new FakeDepartmentRepository(department));

        var asManager = await handler.HandleAsync(
            new SetDepartmentMemberCommand(admin.UserId.Value, department.Id.Value, employee.UserId.Value, true));
        var asMember = await handler.HandleAsync(
            new SetDepartmentMemberCommand(admin.UserId.Value, department.Id.Value, employee.UserId.Value, false));

        Assert.Equal("DepartmentManagerAccessRequired", Assert.Single(asManager.Errors).Code);
        Assert.True(asMember.IsSuccess);
        Assert.True(department.HasMember(employee.UserId));
    }

    [Fact]
    public async Task SetMember_UnknownUserOrDepartment_IsNotFound()
    {
        var admin = Admin();
        var department = NewDepartment();
        var handler = new SetDepartmentMemberHandler(
            new FakeUserRepository(admin), new FakeDepartmentRepository(department));

        var unknownUser = await handler.HandleAsync(
            new SetDepartmentMemberCommand(admin.UserId.Value, department.Id.Value, Guid.NewGuid(), false));
        var unknownDepartment = await handler.HandleAsync(
            new SetDepartmentMemberCommand(admin.UserId.Value, Guid.NewGuid(), admin.UserId.Value, false));

        Assert.Equal("UserNotFound", Assert.Single(unknownUser.Errors).Code);
        Assert.Equal("DepartmentNotFound", Assert.Single(unknownDepartment.Errors).Code);
    }

    [Fact]
    public async Task RemoveMember_RemovesOnlyThatMembership()
    {
        var admin = Admin();
        var employee = NewUser("employee@example.com", UserRole.Employee);
        var service = NewDepartment("Service");
        var sales = NewDepartment("Sales");
        AddMember(service, employee);
        AddMember(sales, employee);
        var handler = new RemoveDepartmentMemberHandler(
            new FakeUserRepository(admin, employee), new FakeDepartmentRepository(service, sales));

        var result = await handler.HandleAsync(
            new RemoveDepartmentMemberCommand(admin.UserId.Value, service.Id.Value, employee.UserId.Value));

        Assert.True(result.IsSuccess);
        Assert.False(service.HasMember(employee.UserId));
        Assert.True(sales.HasMember(employee.UserId));
    }
}

public class DepartmentScopeQueryTests
{
    private sealed class Company
    {
        internal User Admin { get; } = DepartmentTestData.Admin();
        internal User Morgan { get; } = NewUser("morgan@example.com", UserRole.Manager, "Morgan Manager");
        internal User Sam { get; } = NewUser("sam@example.com", UserRole.Manager, "Sam Manager");
        internal User Casey { get; } = NewUser("casey@example.com", UserRole.Employee, "Casey Employee");
        internal User Jordan { get; } = NewUser("jordan@example.com", UserRole.Employee, "Jordan Employee");
        internal User Riley { get; } = NewUser("riley@example.com", UserRole.Employee, "Riley Employee");
        internal Department Service { get; } = NewDepartment("Service");
        internal Department Installation { get; } = NewDepartment("Installation");
        internal Department Sales { get; } = NewDepartment("Sales");

        internal Company()
        {
            // Morgan manages Service and Installation; Sam manages Sales; Jordan is in two departments.
            AddMember(Service, Morgan, isManager: true);
            AddMember(Service, Casey);
            AddMember(Service, Jordan);
            AddMember(Installation, Morgan, isManager: true);
            AddMember(Installation, Jordan);
            AddMember(Sales, Sam, isManager: true);
            AddMember(Sales, Riley);
            AddMember(Sales, Morgan);
            Users = new FakeUserRepository(Admin, Morgan, Sam, Casey, Jordan, Riley);
            Departments = new FakeDepartmentRepository(Service, Installation, Sales);
        }

        internal FakeUserRepository Users { get; }
        internal FakeDepartmentRepository Departments { get; }

        internal Task<Result<IReadOnlyList<UserDto>>> ManagedBy(
            User actor) =>
            new GetManagedEmployeesHandler(Users, Departments).HandleAsync(new GetManagedEmployeesQuery(actor.UserId.Value));

        internal Task<Result<IReadOnlyList<DepartmentDto>>> DepartmentsFor(
            User actor) =>
            new GetDepartmentsHandler(Users, Departments).HandleAsync(new GetDepartmentsQuery(actor.UserId.Value));
    }

    // FR-14: a manager plans work for exactly the employees in the departments they manage.
    [Fact]
    public async Task Manager_SeesExactlyTheMembersOfManagedDepartments()
    {
        var company = new Company();

        var morgan = await company.ManagedBy(company.Morgan);
        var sam = await company.ManagedBy(company.Sam);

        Assert.Equal(new[] { "Casey Employee", "Jordan Employee" }, morgan.Value.Select(user => user.Name));
        // Morgan is a plain member of Sales, so Sam's scope includes Morgan but not Morgan's own employees.
        Assert.Equal(new[] { "Morgan Manager", "Riley Employee" }, sam.Value.Select(user => user.Name));
    }

    [Fact]
    public async Task DeactivatedEmployee_LeavesManagedScope_ButKeepsMembershipHistory()
    {
        var company = new Company();
        Assert.True(company.Casey.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        var managed = await company.ManagedBy(company.Morgan);

        Assert.Equal(new[] { "Jordan Employee" }, managed.Value.Select(user => user.Name));
        Assert.True(company.Service.HasMember(company.Casey.UserId));
    }

    // SEC-01: removing manager access removes department scope at once, even if the flag is still stored.
    [Fact]
    public async Task ManagerDemotedToEmployee_LosesDepartmentScope()
    {
        var company = new Company();
        Assert.True(company.Morgan.ChangeRole(UserRole.Admin, UserStatus.Active, UserRole.Employee).IsSuccess);

        var managed = await company.ManagedBy(company.Morgan);
        var departments = await company.DepartmentsFor(company.Morgan);

        Assert.Equal("ManagedEmployeesForbidden", Assert.Single(managed.Errors).Code);
        Assert.All(departments.Value, department =>
        {
            Assert.False(department.IsManagedByMe);
            Assert.Equal(company.Morgan.UserId.Value, Assert.Single(department.Members).UserId);
        });
    }

    // SEC-01: a job title never grants department scope.
    [Fact]
    public async Task EmployeeWithManagerJobTitle_HasNoManagedEmployees()
    {
        var company = new Company();
        Assert.True(company.Casey.ChangeJobTitle(UserRole.Admin, UserStatus.Active, JobTitle.Create("Manager").Value)
            .IsSuccess);

        var managed = await company.ManagedBy(company.Casey);

        Assert.Equal("ManagedEmployeesForbidden", Assert.Single(managed.Errors).Code);
        Assert.Equal(UserRole.Employee, company.Casey.Role);
    }

    [Fact]
    public async Task Admin_SeesAllDepartmentsAndMembers()
    {
        var company = new Company();

        var result = await company.DepartmentsFor(company.Admin);

        Assert.Equal(new[] { "Installation", "Sales", "Service" }, result.Value.Select(department => department.Name));
        Assert.Equal(3, result.Value.Single(department => department.Name == "Service").Members.Count);
        // Managers are listed first.
        Assert.True(result.Value.Single(department => department.Name == "Service").Members[0].IsManager);
    }

    [Fact]
    public async Task Employee_SeesOwnDepartmentsWithoutColleagues()
    {
        var company = new Company();

        var result = await company.DepartmentsFor(company.Jordan);

        Assert.Equal(new[] { "Installation", "Service" }, result.Value.Select(department => department.Name));
        Assert.All(result.Value, department =>
        {
            Assert.False(department.IsManagedByMe);
            Assert.Equal(company.Jordan.UserId.Value, Assert.Single(department.Members).UserId);
        });
    }

    [Fact]
    public async Task Manager_SeesMembersOnlyInDepartmentsTheyManage()
    {
        var company = new Company();

        var result = await company.DepartmentsFor(company.Morgan);

        var service = result.Value.Single(department => department.Name == "Service");
        var sales = result.Value.Single(department => department.Name == "Sales");
        Assert.True(service.IsManagedByMe);
        Assert.Equal(3, service.Members.Count);
        Assert.False(sales.IsManagedByMe);
        Assert.Equal(company.Morgan.UserId.Value, Assert.Single(sales.Members).UserId);
    }

    [Fact]
    public async Task DeactivatedUser_CannotListDepartments()
    {
        var company = new Company();
        Assert.True(company.Jordan.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        var result = await company.DepartmentsFor(company.Jordan);

        Assert.Equal("DepartmentAccessForbidden", Assert.Single(result.Errors).Code);
    }
}
