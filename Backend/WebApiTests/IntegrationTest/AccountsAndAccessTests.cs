using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Accounts;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Accounts;
using WebAPI.Contracts.Auth;

namespace WebApiTests.IntegrationTest;

// UC-05 Manage accounts and explicit access (FR-05, FR-13, FR-14, SEC-01), through the real HTTP pipeline.
public sealed class AccountsAndAccessTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    // ---- FR-05: local accounts ------------------------------------------------------------------------------

    [Fact]
    public async Task Admin_creates_an_employee_who_can_sign_in_with_baseline_access()
    {
        var admin = await AdminAsync();
        var email = NewEmail();

        var created = await admin.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest("Casey Employee", email, Password, "  Electrician "));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        var employee = await LoginAsync(email, Password);
        var me = await employee.GetFromJsonAsync<EmployeeDto>("/api/employees/me");
        Assert.Equal(id, me!.Id);
        Assert.Equal("Electrician", me.Title);
        Assert.Equal("Active", me.Status);
        Assert.Equal(["Employee"], me.Permissions);
    }

    [Fact]
    public async Task Invalid_accounts_are_rejected()
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        Assert.Equal(HttpStatusCode.Created, (await CreateAsync(admin, email)).StatusCode);

        Assert.Equal("EmailAlreadyInUse", await ErrorAsync(await CreateAsync(admin, email.ToUpperInvariant())));
        Assert.Equal("EmailInvalid", await ErrorAsync(await CreateAsync(admin, "not-an-email")));
        Assert.Equal("PasswordRequired", await ErrorAsync(await CreateAsync(admin, NewEmail(), password: " ")));
        Assert.Equal(HttpStatusCode.BadRequest, (await CreateAsync(admin, NewEmail(), password: "short")).StatusCode);
        Assert.Equal("InvalidSystemPermission", await ErrorAsync(await CreateAsync(admin, NewEmail(), permissions: ["Owner"])));
        Assert.Equal("InvalidSystemPermission", await ErrorAsync(await CreateAsync(admin, NewEmail(), permissions: ["99"])));
        Assert.Equal("TitleTooLong", await ErrorAsync(await CreateAsync(admin, NewEmail(), title: new string('a', 101))));
    }

    [Fact]
    public async Task Deactivated_employee_cannot_sign_in_or_use_an_existing_token_and_is_kept()
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        var id = await CreateEmployeeAsync(admin, email);
        var employee = await LoginAsync(email, Password);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/employees/{id}/deactivate", null)).StatusCode);

        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Equal("UserInactive", await ErrorAsync(login));
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/employees/me")).StatusCode);

        // The account stays in the system so its history can still be shown.
        var kept = await admin.GetFromJsonAsync<EmployeeDto>($"/api/employees/{id}");
        Assert.Equal("Deactivated", kept!.Status);
        Assert.Equal("EmployeeAlreadyDeactivated", await ErrorAsync(await admin.PostAsync($"/api/employees/{id}/deactivate", null)));

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/employees/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Anonymous().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password))).StatusCode);
    }

    [Fact]
    public async Task Admin_sets_a_new_password_that_replaces_the_old_one()
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        var id = await CreateEmployeeAsync(admin, email);
        const string newPassword = "N3w-Passw0rd!";

        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PutAsJsonAsync($"/api/employees/{id}/password", new SetEmployeePasswordRequest(newPassword))).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Anonymous().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await Anonymous().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, newPassword))).StatusCode);
    }

    [Fact]
    public async Task Invalid_new_passwords_are_rejected_and_keep_the_old_password()
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        var id = await CreateEmployeeAsync(admin, email);

        Assert.Equal("PasswordRequired", await ErrorAsync(
            await admin.PutAsJsonAsync($"/api/employees/{id}/password", new SetEmployeePasswordRequest(" "))));
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"/api/employees/{id}/password", new SetEmployeePasswordRequest("short"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.PutAsJsonAsync($"/api/employees/{Guid.NewGuid()}/password", new SetEmployeePasswordRequest("N3w-Passw0rd!"))).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await Anonymous().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password))).StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_lock_themselves_out()
    {
        var admin = await AdminAsync();
        var me = await admin.GetFromJsonAsync<EmployeeDto>("/api/employees/me");

        var deactivate = await admin.PostAsync($"/api/employees/{me!.Id}/deactivate", null);
        var removeAdmin = await admin.PutAsJsonAsync($"/api/employees/{me.Id}/permissions",
            new SetEmployeePermissionsRequest(["Employee"]));

        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
        Assert.Equal("SelfDeactivationForbidden", await ErrorAsync(deactivate));
        Assert.Equal("SelfAdministrationRemovalForbidden", await ErrorAsync(removeAdmin));
        Assert.Contains("Administrator", (await admin.GetFromJsonAsync<EmployeeDto>("/api/employees/me"))!.Permissions);
    }

    // ---- FR-13: system access independent of the displayed title --------------------------------------------

    [Fact]
    public async Task Title_grants_no_access()
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        await CreateEmployeeAsync(admin, email, title: "Administrator");
        var employee = await LoginAsync(email, Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/departments/managed-employees")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest("Mine"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/exports")).StatusCode);
    }

    [Fact]
    public async Task Changing_the_title_keeps_access_and_changing_access_keeps_the_title()
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        var id = await CreateEmployeeAsync(admin, email, title: "Electrician");

        var profile = await admin.PutAsJsonAsync($"/api/employees/{id}", new UpdateEmployeeRequest("Casey Renamed", "Project lead"));
        Assert.Equal(HttpStatusCode.NoContent, profile.StatusCode);
        var afterProfile = await admin.GetFromJsonAsync<EmployeeDto>($"/api/employees/{id}");
        Assert.Equal(("Casey Renamed", "Project lead"), (afterProfile!.Name, afterProfile.Title));
        Assert.Equal(["Employee"], afterProfile.Permissions);

        var access = await admin.PutAsJsonAsync($"/api/employees/{id}/permissions",
            new SetEmployeePermissionsRequest(["Employee", "Payroll"]));
        Assert.Equal(HttpStatusCode.NoContent, access.StatusCode);
        var afterAccess = await admin.GetFromJsonAsync<EmployeeDto>($"/api/employees/{id}");
        Assert.Equal("Project lead", afterAccess!.Title);
        Assert.Equal(["Payroll", "Employee"], afterAccess.Permissions);

        var clear = await admin.PutAsJsonAsync($"/api/employees/{id}", new UpdateEmployeeRequest("Casey Renamed", " "));
        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);
        Assert.Null((await admin.GetFromJsonAsync<EmployeeDto>($"/api/employees/{id}"))!.Title);
    }

    [Fact]
    public async Task Granted_and_revoked_access_applies_immediately_to_an_existing_token()
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        var id = await CreateEmployeeAsync(admin, email);
        var employee = await LoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/exports")).StatusCode);

        await admin.PutAsJsonAsync($"/api/employees/{id}/permissions", new SetEmployeePermissionsRequest(["Employee", "Payroll"]));
        Assert.Equal(HttpStatusCode.OK, (await employee.GetAsync("/api/exports")).StatusCode);

        await admin.PutAsJsonAsync($"/api/employees/{id}/permissions", new SetEmployeePermissionsRequest(["Employee"]));
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/exports")).StatusCode);

        // Without the baseline Employee access the account can no longer sign in.
        await admin.PutAsJsonAsync($"/api/employees/{id}/permissions", new SetEmployeePermissionsRequest([]));
        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    // ---- FR-14: departments -------------------------------------------------------------------------------

    [Fact]
    public async Task Managers_plan_for_exactly_the_members_of_departments_they_manage()
    {
        var admin = await AdminAsync();
        var managerEmail = NewEmail();
        var manager = await CreateEmployeeAsync(admin, managerEmail, permissions: ["DepartmentManager"]);
        var inService = await CreateEmployeeAsync(admin, NewEmail(), name: "Ann Service");
        var inBoth = await CreateEmployeeAsync(admin, NewEmail(), name: "Bo Both");
        var elsewhere = await CreateEmployeeAsync(admin, NewEmail(), name: "Cy Elsewhere");
        var service = await CreateDepartmentAsync(admin);
        var installation = await CreateDepartmentAsync(admin);
        var sales = await CreateDepartmentAsync(admin);

        await PutAsync(admin, $"/api/departments/{service}/managers/{manager}");
        await PutAsync(admin, $"/api/departments/{installation}/managers/{manager}");
        await PutAsync(admin, $"/api/departments/{service}/employees/{inService}");
        await PutAsync(admin, $"/api/departments/{service}/employees/{inBoth}");
        await PutAsync(admin, $"/api/departments/{installation}/employees/{inBoth}");
        await PutAsync(admin, $"/api/departments/{installation}/employees/{manager}");
        await PutAsync(admin, $"/api/departments/{sales}/employees/{elsewhere}");

        var client = await LoginAsync(managerEmail, Password);
        // The manager's own membership and other departments are not included.
        Assert.Equal(new[] { inService, inBoth }.Order(), (await ManagedAsync(client)).Select(person => person.EmployeeId).Order());

        // Removing a membership removes the employee from the manager's scope.
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/departments/{service}/employees/{inService}")).StatusCode);
        Assert.Equal(inBoth, Assert.Single(await ManagedAsync(client)).EmployeeId);
    }

    [Fact]
    public async Task Department_manager_needs_the_grant_not_the_title()
    {
        var admin = await AdminAsync();
        var titled = await CreateEmployeeAsync(admin, NewEmail(), title: "Department manager");
        var department = await CreateDepartmentAsync(admin);

        var response = await admin.PutAsync($"/api/departments/{department}/managers/{titled}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("DepartmentManagerAccessRequired", await ErrorAsync(response));
        Assert.Empty(Assert.Single(await DepartmentsAsync(admin), item => item.Id == department).Managers);
    }

    [Fact]
    public async Task Revoked_or_deactivated_people_leave_the_managed_scope_but_links_are_kept()
    {
        var admin = await AdminAsync();
        var managerEmail = NewEmail();
        var manager = await CreateEmployeeAsync(admin, managerEmail, permissions: ["DepartmentManager"]);
        var active = await CreateEmployeeAsync(admin, NewEmail());
        var leaving = await CreateEmployeeAsync(admin, NewEmail());
        var department = await CreateDepartmentAsync(admin);
        await PutAsync(admin, $"/api/departments/{department}/managers/{manager}");
        await PutAsync(admin, $"/api/departments/{department}/employees/{active}");
        await PutAsync(admin, $"/api/departments/{department}/employees/{leaving}");
        var client = await LoginAsync(managerEmail, Password);

        await admin.PostAsync($"/api/employees/{leaving}/deactivate", null);
        Assert.Equal(active, Assert.Single(await ManagedAsync(client)).EmployeeId);
        var members = Assert.Single(await DepartmentsAsync(admin), item => item.Id == department).Members;
        Assert.Equal("Deactivated", Assert.Single(members, person => person.EmployeeId == leaving).Status);

        await admin.PutAsJsonAsync($"/api/employees/{manager}/permissions", new SetEmployeePermissionsRequest(["Employee"]));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/departments/managed-employees")).StatusCode);
        Assert.False(Assert.Single(await DepartmentsAsync(client)).ManagedByMe);
    }

    [Fact]
    public async Task Department_lists_only_show_what_each_role_may_see()
    {
        var admin = await AdminAsync();
        var managerEmail = NewEmail();
        var employeeEmail = NewEmail();
        var manager = await CreateEmployeeAsync(admin, managerEmail, permissions: ["DepartmentManager"]);
        var employee = await CreateEmployeeAsync(admin, employeeEmail);
        var colleague = await CreateEmployeeAsync(admin, NewEmail());
        var department = await CreateDepartmentAsync(admin);
        var other = await CreateDepartmentAsync(admin);
        await PutAsync(admin, $"/api/departments/{department}/managers/{manager}");
        await PutAsync(admin, $"/api/departments/{department}/employees/{employee}");
        await PutAsync(admin, $"/api/departments/{department}/employees/{colleague}");

        var asManager = Assert.Single(await DepartmentsAsync(await LoginAsync(managerEmail, Password)));
        Assert.True(asManager.ManagedByMe);
        Assert.Equal(2, asManager.Members.Count);

        var asEmployee = Assert.Single(await DepartmentsAsync(await LoginAsync(employeeEmail, Password)));
        Assert.False(asEmployee.ManagedByMe);
        Assert.Equal(employee, Assert.Single(asEmployee.Members).EmployeeId);

        var asAdmin = await DepartmentsAsync(admin);
        Assert.Contains(asAdmin, item => item.Id == other);
        Assert.Equal(2, Assert.Single(asAdmin, item => item.Id == department).Members.Count);
    }

    [Fact]
    public async Task Department_validation_and_missing_resources()
    {
        var admin = await AdminAsync();
        var name = "Dept " + Guid.NewGuid().ToString("N");
        var department = await CreateDepartmentAsync(admin, name);
        var employee = await CreateEmployeeAsync(admin, NewEmail());

        Assert.Equal("DepartmentNameTaken", await ErrorAsync(await admin.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest(name))));
        Assert.Equal("DepartmentNameRequired", await ErrorAsync(await admin.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest(" "))));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsync($"/api/departments/{Guid.NewGuid()}/employees/{employee}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsync($"/api/departments/{department}/employees/{Guid.NewGuid()}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/departments/{department}/employees/{employee}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/departments/{department}/managers/{employee}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/employees/{Guid.NewGuid()}")).StatusCode);

        // Adding the same member twice is harmless.
        await PutAsync(admin, $"/api/departments/{department}/employees/{employee}");
        await PutAsync(admin, $"/api/departments/{department}/employees/{employee}");
        Assert.Single(Assert.Single(await DepartmentsAsync(admin), item => item.Id == department).Members);
    }

    // ---- SEC-01: the API refuses administration outside the caller's access -------------------------------

    [Theory]
    [InlineData("Employee")]
    [InlineData("DepartmentManager")]
    [InlineData("Payroll")]
    [InlineData("CatagoryReviewer")]
    public async Task Non_administrators_cannot_administer_accounts_or_departments(string permission)
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        var actorId = await CreateEmployeeAsync(admin, email, permissions: [permission]);
        var target = await CreateEmployeeAsync(admin, NewEmail());
        var department = await CreateDepartmentAsync(admin);
        var actor = await LoginAsync(email, Password);

        var responses = new[]
        {
            await actor.GetAsync("/api/employees"),
            await actor.GetAsync($"/api/employees/{target}"),
            await actor.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest("X", NewEmail(), Password)),
            await actor.PutAsJsonAsync($"/api/employees/{target}", new UpdateEmployeeRequest("X", "Boss")),
            await actor.PutAsJsonAsync($"/api/employees/{actorId}/permissions", new SetEmployeePermissionsRequest(["Employee", "Administrator"])),
            await actor.PostAsync($"/api/employees/{target}/deactivate", null),
            await actor.PutAsJsonAsync($"/api/employees/{target}/password", new SetEmployeePasswordRequest(Password)),
            await actor.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest("Mine")),
            await actor.PutAsync($"/api/departments/{department}/employees/{target}", null),
            await actor.PutAsync($"/api/departments/{department}/managers/{actorId}", null),
            await actor.DeleteAsync($"/api/departments/{department}/employees/{target}"),
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        // The attempted self-promotion did not change anything.
        Assert.DoesNotContain("Administrator", (await admin.GetFromJsonAsync<EmployeeDto>($"/api/employees/{actorId}"))!.Permissions);
    }

    [Theory]
    [InlineData("/api/employees/me")]
    [InlineData("/api/employees")]
    [InlineData("/api/departments")]
    [InlineData("/api/departments/managed-employees")]
    public async Task Account_endpoints_require_sign_in(string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync(url)).StatusCode);

    // ---- helpers ------------------------------------------------------------------------------------------

    private Task<HttpClient> AdminAsync() => LoginAsync(TestWebAppFactory.AdminEmail, TestWebAppFactory.AdminPassword);

    private static string NewEmail() => $"employee-{Guid.NewGuid():N}@test.local";

    private static Task<HttpResponseMessage> CreateAsync(HttpClient admin, string email, string? password = null,
        string? title = null, string[]? permissions = null) =>
        admin.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest("Test Employee", email, password ?? Password, title, permissions));

    private static async Task<Guid> CreateEmployeeAsync(HttpClient admin, string email, string? title = null,
        string[]? permissions = null, string name = "Test Employee")
    {
        var response = await admin.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest(name, email, Password, title, permissions));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static async Task<Guid> CreateDepartmentAsync(HttpClient admin, string? name = null)
    {
        var response = await admin.PostAsJsonAsync("/api/departments",
            new CreateDepartmentRequest(name ?? "Dept " + Guid.NewGuid().ToString("N")));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static async Task PutAsync(HttpClient admin, string url) =>
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsync(url, null)).StatusCode);

    private static async Task<IReadOnlyList<DepartmentDto>> DepartmentsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ItemsResponse<DepartmentDto>>("/api/departments"))!.Items;

    private static async Task<IReadOnlyList<DepartmentPersonDto>> ManagedAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ItemsResponse<DepartmentPersonDto>>("/api/departments/managed-employees"))!.Items;

    private static async Task<string?> ErrorAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.EnumerateArray().First().GetProperty("code").GetString();
    }
}
