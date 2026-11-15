using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Persistence;

namespace WebApiTests.IntegrationTest;

public sealed class CommandTransactionRepositoryTests
{
    [Fact]
    public async Task A_serialization_retry_reloads_fresh_aggregate_state_and_reruns_the_operation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var fault = new OneSerializationFailure();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).AddInterceptors(fault).Options);
        await context.Database.EnsureCreatedAsync();
        var employee = Employee.Create("Original", "worker@test.invalid", DateTime.UtcNow).Value;
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        fault.Armed = true;
        var loaded = new List<Employee>();

        var result = await new CommandTransaction(context).ExecuteAsync(async ct =>
        {
            var fresh = await context.Employees.SingleAsync(item => item.Id == employee.Id, ct);
            Assert.Equal("Original", fresh.Name.Value);
            loaded.Add(fresh);
            Assert.True(fresh.ChangeProfile(PersonName.Create($"Attempt {loaded.Count}").Value, fresh.Email).IsSuccess);
            return Result<bool>.Success(true);
        }, "ExportClosureConflict");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, loaded.Count);
        Assert.NotSame(loaded[0], loaded[1]);
        Assert.Equal("Attempt 2", (await context.Employees.AsNoTracking().SingleAsync()).Name.Value);
    }

    private sealed class OneSerializationFailure : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Armed)
            {
                Armed = false;
                throw new PostgresException("Injected serialization conflict", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure);
            }
            return ValueTask.FromResult(result);
        }
    }
}
