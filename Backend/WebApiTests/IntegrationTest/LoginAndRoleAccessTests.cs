using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Auth;

namespace WebApiTests.IntegrationTest;

// Sign-in and authentication of protected endpoints.
public class LoginAndRoleAccessTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Login_with_valid_credentials_returns_bearer_token()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestWebAppFactory.AdminEmail, TestWebAppFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(login!.AccessToken));
        Assert.Equal("Bearer", login.TokenType, ignoreCase: true);
        Assert.True(login.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestWebAppFactory.AdminEmail, "Wrong-Password1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@test.local", ApiTestBase.Password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_is_available_without_token()
    {
        var response = await Anonymous().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/time_entries?fromDate=2026-03-02&toDate=2026-03-08")]
    [InlineData("/api/time_entries/reviewer")]
    [InlineData("/api/exports")]
    [InlineData("/api/reports/top-categories")]
    public async Task Protected_endpoints_without_token_return_401(string url)
    {
        var response = await Anonymous().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_token_returns_401()
    {
        var client = Anonymous();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/time_entries?fromDate=2026-03-02&toDate=2026-03-08");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
