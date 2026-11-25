using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Auth;

namespace WebApiTests.IntegrationTest;

// Shared helpers for HTTP tests against the API with an in-memory database and a seeded admin.
public abstract class ApiTestBase : IClassFixture<TestWebAppFactory>
{
    public const string Password = "Passw0rd!123";

    protected readonly TestWebAppFactory Factory;

    protected ApiTestBase(TestWebAppFactory factory) => Factory = factory;

    protected HttpClient Anonymous() => Factory.CreateClient();

    protected async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }
}
