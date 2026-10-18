using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Authentication;

namespace UnitTests.Auth;

// Starts the real API in memory. Every request here is rejected or answered before a
// handler runs, so no database is needed.
public class AuthorizationTests : IClassFixture<AuthorizationTests.ApiFactory>
{
    private const string Secret = "test-secret-that-is-at-least-32-bytes-long";

    private readonly HttpClient _client;
    private readonly IAccessTokenGenerator _tokenGenerator;

    public AuthorizationTests(ApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        _tokenGenerator = factory.Services.GetRequiredService<IAccessTokenGenerator>();
    }

    [Fact]
    public async Task Health_WithoutToken_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenSignedByAnotherKey_Returns401()
    {
        var token = CreateTokenWithKey("another-secret-that-is-at-least-32-bytes");

        var response = await SendAsync(HttpMethod.Get, "/api/users/me", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/orders")]
    [InlineData("/api/users")]
    public async Task ManagerEndpoint_WithEmployeeToken_Returns403(string path)
    {
        var response = await SendAsync(HttpMethod.Post, path, CreateToken(UserRole.Employee));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Employee)]
    [InlineData(UserRole.Manager)]
    public async Task AdminEndpoint_WithoutAdminRole_Returns403(UserRole role)
    {
        var path = $"/api/users/{Guid.NewGuid()}/role";

        var response = await SendAsync(HttpMethod.Post, path, CreateToken(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_Returns401WithoutToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "not-an-email", password = "Some-Password-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("InvalidCredentials", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = method == HttpMethod.Get ? null : JsonContent.Create(new { })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }

    // Uses the API's own token generator, so the token is valid apart from its role.
    private string CreateToken(UserRole role)
    {
        var user = User.Create(
            PersonName.Create("Test User").Value,
            EmailAddress.Create("test.user@example.com").Value,
            role).Value;

        return _tokenGenerator.Generate(user).Value;
    }

    private static string CreateTokenWithKey(string secret) => new JsonWebTokenHandler().CreateToken(
        new SecurityTokenDescriptor
        {
            Issuer = "ordreflow-api",
            Audience = "ordreflow-frontend",
            Expires = DateTime.UtcNow.AddHours(1),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                new Claim(JwtTokenGenerator.RoleClaimType, nameof(UserRole.Admin))
            }),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                SecurityAlgorithms.HmacSha256)
        });

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", Secret);
            Environment.SetEnvironmentVariable("JWT_ISSUER", null);
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
            // Skips the admin seed, which would need a database.
            Environment.SetEnvironmentVariable("ADMIN_EMAIL", null);
            Environment.SetEnvironmentVariable("ADMIN_PASSWORD", null);
        }
    }
}
