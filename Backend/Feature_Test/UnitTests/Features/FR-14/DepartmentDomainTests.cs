using Domain.Aggregate;
using Domain.ValueObjects;
using static UnitTests.Features.FR14.DepartmentTestData;

namespace UnitTests.Features.FR14;

public class DepartmentDomainTests
{
    [Fact]
    public void ActiveAdmin_CreatesDepartmentWithTrimmedName()
    {
        var result = Department.Create(UserRole.Admin, UserStatus.Active, DepartmentName.Create("  Service  ").Value);

        Assert.True(result.IsSuccess);
        Assert.Equal("Service", result.Value.Name.Value);
        Assert.Empty(result.Value.Members);
    }

    // Only an active admin can manage departments (SEC-01).
    [Theory]
    [InlineData(UserRole.Employee, UserStatus.Active)]
    [InlineData(UserRole.Manager, UserStatus.Active)]
    [InlineData(UserRole.Admin, UserStatus.Inactive)]
    public void NonAdminOrInactiveAdmin_CannotManageDepartments(UserRole role, UserStatus status)
    {
        var department = NewDepartment();
        var employee = NewUser("employee@example.com", UserRole.Employee);

        var create = Department.Create(role, status, DepartmentName.Create("Sales").Value);
        var rename = department.Rename(role, status, DepartmentName.Create("Sales").Value);
        var add = department.SetMember(role, status, employee.UserId, employee.Role, employee.Status, false);

        Assert.Equal("DepartmentManagementForbidden", Assert.Single(create.Errors).Code);
        Assert.Equal("DepartmentManagementForbidden", Assert.Single(rename.Errors).Code);
        Assert.Equal("DepartmentManagementForbidden", Assert.Single(add.Errors).Code);
        Assert.Equal("Service", department.Name.Value);
        Assert.Empty(department.Members);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BlankDepartmentName_IsRejected(string? name)
    {
        Assert.Equal("DepartmentNameRequired", Assert.Single(DepartmentName.Create(name).Errors).Code);
    }

    [Fact]
    public void DepartmentNameLongerThanLimit_IsRejected()
    {
        var result = DepartmentName.Create(new string('a', DepartmentName.MaxLength + 1));

        Assert.Equal("DepartmentNameTooLong", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void Admin_AddsEmployeeAndManager_AndCanToggleManagerFlag()
    {
        var department = NewDepartment();
        var employee = NewUser("employee@example.com", UserRole.Employee);
        var manager = NewUser("manager@example.com", UserRole.Manager);

        AddMember(department, employee);
        AddMember(department, manager, isManager: true);

        Assert.Equal(2, department.Members.Count);
        Assert.True(department.IsManagedBy(manager.UserId));
        Assert.False(department.IsManagedBy(employee.UserId));

        // Setting an existing member again updates the flag instead of adding a duplicate.
        AddMember(department, manager, isManager: false);

        Assert.Equal(2, department.Members.Count);
        Assert.False(department.IsManagedBy(manager.UserId));
        Assert.True(department.HasMember(manager.UserId));
    }

    // FR-13/SEC-01: department management requires manager or admin system access.
    [Fact]
    public void EmployeeAccess_CannotBeDepartmentManager()
    {
        var department = NewDepartment();
        var employee = NewUser("employee@example.com", UserRole.Employee);
        Assert.True(employee.ChangeJobTitle(UserRole.Admin, UserStatus.Active, JobTitle.Create("Team lead").Value)
            .IsSuccess);

        var result = department.SetMember(
            UserRole.Admin, UserStatus.Active, employee.UserId, employee.Role, employee.Status, isManager: true);

        Assert.Equal("DepartmentManagerAccessRequired", Assert.Single(result.Errors).Code);
        Assert.Empty(department.Members);
    }

    [Fact]
    public void InactiveUser_CannotBeAddedToDepartment()
    {
        var department = NewDepartment();
        var user = NewUser("former@example.com", UserRole.Employee);
        Assert.True(user.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        var result = department.SetMember(
            UserRole.Admin, UserStatus.Active, user.UserId, user.Role, user.Status, isManager: false);

        Assert.Equal("DepartmentMemberInactive", Assert.Single(result.Errors).Code);
        Assert.Empty(department.Members);
    }

    [Fact]
    public void UserCanBelongToSeveralDepartments()
    {
        var service = NewDepartment("Service");
        var installation = NewDepartment("Installation");
        var manager = NewUser("manager@example.com", UserRole.Manager);

        AddMember(service, manager, isManager: true);
        AddMember(installation, manager);

        Assert.True(service.IsManagedBy(manager.UserId));
        Assert.True(installation.HasMember(manager.UserId));
        Assert.False(installation.IsManagedBy(manager.UserId));
    }

    [Fact]
    public void Admin_RemovesMember_AndRemovingUnknownMemberFails()
    {
        var department = NewDepartment();
        var employee = NewUser("employee@example.com", UserRole.Employee);
        AddMember(department, employee);

        var removed = department.RemoveMember(UserRole.Admin, UserStatus.Active, employee.UserId);
        var again = department.RemoveMember(UserRole.Admin, UserStatus.Active, employee.UserId);

        Assert.True(removed.IsSuccess);
        Assert.Empty(department.Members);
        Assert.Equal("DepartmentMemberNotFound", Assert.Single(again.Errors).Code);
    }

    [Fact]
    public void Manager_CannotRemoveMembers()
    {
        var department = NewDepartment();
        var employee = NewUser("employee@example.com", UserRole.Employee);
        AddMember(department, employee);

        var result = department.RemoveMember(UserRole.Manager, UserStatus.Active, employee.UserId);

        Assert.Equal("DepartmentManagementForbidden", Assert.Single(result.Errors).Code);
        Assert.True(department.HasMember(employee.UserId));
    }
}
