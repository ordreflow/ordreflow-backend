using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Persistence;

namespace WebApiTests.IntegrationTest.Factories;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Passw0rd!123";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public TestWebAppFactory()
    {
        // Program reads these while building the host; the admin is seeded at startup.
        Environment.SetEnvironmentVariable("JWT_SECRET", new string('x', 64));
        Environment.SetEnvironmentVariable("ADMIN_EMAIL", AdminEmail);
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", AdminPassword);
        Environment.SetEnvironmentVariable("ADMIN_NAME", "Test Admin");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            // The in-memory database lives as long as the connection is open.
            _connection.Open();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}