using Domain.Aggregate;
using Domain.Services;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

// UC-05 (FR-13, FR-14, SEC-01): rules for placing employees in departments.
public sealed class DepartmentStaffingDomainTests
{
    private readonly DepartmentStaffingDomainService _staffing = new();

    private static Department NewDepartment() => Department.Create(DepartmentName.Create("Service").Value).Value;

    private static Employee NewEmployee(string? title = null)
    {
        var employee = Employee.Create("Casey Employee", $"casey-{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
        Assert.True(employee.ChangeProfile(employee.Name, employee.Email, title).IsSuccess);
        return employee;
    }

    [Fact]
    public void Active_employee_becomes_member()
    {
        var department = NewDepartment();
        var employee = NewEmployee();

        var membership = _staffing.AddMember(department, employee);

        Assert.True(membership.IsSuccess);
        Assert.Equal(department.Id, membership.Value.DepartmentId);
        Assert.Equal(employee.Id, membership.Value.EmployeeId);
    }

    [Fact]
    public void Deactivated_employee_cannot_be_placed()
    {
        var employee = NewEmployee();
        Assert.True(employee.GrantPermission(SystemPermission.DepartmentManager).IsSuccess);
        employee.Deactivate();

        Assert.Equal("EmployeeInactive", Assert.Single(_staffing.AddMember(NewDepartment(), employee).Errors).Code);
        Assert.Equal("EmployeeInactive", Assert.Single(_staffing.AppointManager(NewDepartment(), employee).Errors).Code);
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Department manager")]
    [InlineData("Administrator")]
    public void Title_never_grants_department_management(string title)
    {
        var employee = NewEmployee(title);

        var management = _staffing.AppointManager(NewDepartment(), employee);

        Assert.Equal("DepartmentManagerAccessRequired", Assert.Single(management.Errors).Code);
        Assert.False(DepartmentStaffingDomainService.HasManagerAccess(employee));
    }

    [Fact]
    public void Department_manager_grant_allows_management_until_it_is_revoked()
    {
        var employee = NewEmployee();
        Assert.True(employee.GrantPermission(SystemPermission.DepartmentManager).IsSuccess);

        var management = _staffing.AppointManager(NewDepartment(), employee);
        Assert.True(management.IsSuccess);
        Assert.True(DepartmentStaffingDomainService.HasManagerAccess(employee));

        Assert.True(employee.RevokePermission(SystemPermission.DepartmentManager).IsSuccess);
        Assert.False(DepartmentStaffingDomainService.HasManagerAccess(employee));
    }

    [Fact]
    public void Administrator_without_department_manager_grant_cannot_manage_a_department()
    {
        var employee = NewEmployee();
        Assert.True(employee.GrantPermission(SystemPermission.Administrator).IsSuccess);

        Assert.Equal("DepartmentManagerAccessRequired",
            Assert.Single(_staffing.AppointManager(NewDepartment(), employee).Errors).Code);
    }
}
