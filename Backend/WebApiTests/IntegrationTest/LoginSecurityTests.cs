using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Persistence.Identity;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Auth;

namespace WebApiTests.IntegrationTest;

// UC-06 Sign in locally (FR-06, SEC-04): rate limiting, lockout, hashed passwords, no secrets in logs, HSTS.
public class LoginSecurityTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Account_is_locked_after_five_wrong_passwords_without_revealing_it()
    {
        var admin = await AdminAsync();
        var employee = await CreateUserAsync(admin, "Employee");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var wrong = await LoginRequestAsync(employee.Email, "Wrong-Password1!");
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        var correct = await LoginRequestAsync(employee.Email, Password);

        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
        Assert.Equal("InvalidCredentials", await ErrorCodeAsync(correct));
    }

    [Fact]
    public async Task Passwords_are_stored_as_hashes()
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var account = await users.FindByEmailAsync(TestWebAppFactory.AdminEmail);

        Assert.NotNull(account!.PasswordHash);
        Assert.DoesNotContain(TestWebAppFactory.AdminPassword, account.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            users.PasswordHasher.VerifyHashedPassword(account, account.PasswordHash, TestWebAppFactory.AdminPassword));
    }

    private Task<HttpResponseMessage> LoginRequestAsync(string email, string password) =>
        Anonymous().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

    internal static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.EnumerateArray().Single().GetProperty("code").GetString();
    }
}

public class LoginRateLimitTests(LoginRateLimitTests.LowLimitFactory factory) : IClassFixture<LoginRateLimitTests.LowLimitFactory>
{
    public sealed class LowLimitFactory : TestWebAppFactory
    {
        protected override int LoginPermitLimit => 3;
    }

    [Fact]
    public async Task Too_many_attempts_from_one_client_return_429_with_structured_error()
    {
        var client = ClientFrom("203.0.113.10");

        for (var attempt = 0; attempt < 3; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await WrongLoginAsync(client)).StatusCode);

        var limited = await WrongLoginAsync(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.RetryAfter is not null);
        Assert.Equal("TooManyLoginAttempts", await LoginSecurityTests.ErrorCodeAsync(limited));
    }

    [Fact]
    public async Task Limit_is_per_client_ip_and_only_on_login()
    {
        var busy = ClientFrom("203.0.113.20");
        for (var attempt = 0; attempt < 4; attempt++)
            await WrongLoginAsync(busy);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await WrongLoginAsync(busy)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await WrongLoginAsync(ClientFrom("203.0.113.21"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await busy.GetAsync("/api/health")).StatusCode);
    }

    // The API runs behind proxies, so the client IP comes from X-Forwarded-For.
    private HttpClient ClientFrom(string ip)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
        return client;
    }

    private static Task<HttpResponseMessage> WrongLoginAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/auth/login", new LoginRequest(TestWebAppFactory.AdminEmail, "Wrong-Password1!"));
}

public class LoginLoggingTests(LoginLoggingTests.CapturingFactory factory) : IClassFixture<LoginLoggingTests.CapturingFactory>
{
    public sealed class CapturingFactory : TestWebAppFactory
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddProvider(new CapturingLoggerProvider(Messages));
            });
        }
    }

    [Fact]
    public async Task Logs_contain_no_passwords_or_tokens()
    {
        const string wrongPassword = "Wrong-Secret-Password-42!";
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(TestWebAppFactory.AdminEmail, wrongPassword));
        var success = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestWebAppFactory.AdminEmail, TestWebAppFactory.AdminPassword));
        var token = (await success.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        Assert.NotEmpty(factory.Messages);
        Assert.DoesNotContain(factory.Messages, message => message.Contains(wrongPassword));
        Assert.DoesNotContain(factory.Messages, message => message.Contains(TestWebAppFactory.AdminPassword));
        Assert.DoesNotContain(factory.Messages, message => message.Contains(token));
    }

    private sealed class CapturingLoggerProvider(ConcurrentQueue<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(messages);
        public void Dispose() { }
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            messages.Enqueue($"{formatter(state, exception)} {state} {exception}");
    }
}

public class HstsTests(HstsTests.ProductionFactory factory) : IClassFixture<HstsTests.ProductionFactory>
{
    public sealed class ProductionFactory : TestWebAppFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Production");
        }
    }

    [Fact]
    public async Task Https_responses_outside_development_include_hsts()
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://ordreflow.test") });

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Strict-Transport-Security", response.Headers.Select(header => header.Key));
    }
}
