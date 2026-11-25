using Domain.Aggregate;
using Catagory = Domain.Entities.Catagory;
using CatagoryReviewer = Domain.Entities.CatagoryReviewer;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;
using Domain.Interfaces.IUnitOfWork;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace WebApiTests.IntegrationTest;

public sealed class ReviewDecisionRepositoryTests
{
    [Theory]
    [InlineData("review")]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task Scope_uses_the_task_category_and_a_stale_write_cannot_overwrite_approval(string competingAction)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var reviewer = Employee.Create("Reviewer", "reviewer@test.invalid", DateTime.UtcNow).Value;
        var worker = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        Assert.True(reviewer.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        var department = Department.Create(DepartmentName.Create("Work department").Value).Value;
        var allowed = department.AddCatagory(worker.Id, CatagoryName.Create("Assigned").Value, false, DateTime.UtcNow).Value;
        var other = department.AddCatagory(worker.Id, CatagoryName.Create("Other").Value, false, DateTime.UtcNow).Value;
        var allowedTask = department.AddTask(allowed.Id, TaskName.Create("Allowed").Value, DateTime.UtcNow).Value;
        var otherTask = department.AddTask(other.Id, TaskName.Create("Other").Value, DateTime.UtcNow).Value;
        var initialTime = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var date = WorkDate.Create(new DateOnly(2026, 10, 1)).Value;
        var entry = TimeEntry.Record(worker.Id, allowedTask.Id, date, Duration.Create(2m).Value,
            TimeEntryDescription.Create("Work").Value, WorkDatePeriod.Create(date, date).Value, initialTime).Value;
        var id = entry.Id;

        await using (var seed = new AppDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Employees.AddRange(reviewer, worker);
            seed.Departments.Add(department);
            seed.Catagories.AddRange(allowed, other);
            seed.Tasks.AddRange(allowedTask, otherTask);
            seed.CatagoryReviewers.Add(new CatagoryReviewer
            {
                EmployeeId = reviewer.Id, CatagoryId = allowed.Id
            });
            seed.TimeEntries.Add(entry);
            await seed.SaveChangesAsync();
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var firstRepository = new ReviewDecisionRepository(firstContext);
        var secondRepository = new ReviewDecisionRepository(secondContext);
        Assert.True(await firstRepository.HasReviewerTaskAccessAsync(reviewer.Id, allowedTask.Id));
        Assert.False(await firstRepository.HasReviewerTaskAccessAsync(reviewer.Id, otherTask.Id));
        Assert.False(await firstRepository.HasReviewerTaskAccessAsync(worker.Id, allowedTask.Id));

        var first = (await firstRepository.GetForDecisionAsync(id))!;
        var second = (await secondRepository.GetForDecisionAsync(id))!;
        Assert.Single(first.Audits);
        Assert.Empty(first.Reviews);
        Assert.True(first.Approve(reviewer.Id, initialTime.AddMinutes(1), allowSelfReview: true).IsSuccess);
        var current = second.GetCurrentAudit()!;
        var period = WorkDatePeriod.Create(current.WorkDate, current.WorkDate).Value;
        var competingWrite = competingAction switch
        {
            "edit" => second.Change(worker.Id, current.WorkDate, Duration.Create(4m).Value,
                current.Description, period, initialTime.AddMinutes(2)),
            "delete" => second.Delete(worker.Id, period, initialTime.AddMinutes(2)),
            _ => second.Reject(reviewer.Id, "Incorrect hours", initialTime.AddMinutes(2), allowSelfReview: true)
        };
        Assert.True(competingWrite.IsSuccess);

        await new UnitOfWork(firstContext).SaveChangesAsync();
        await Assert.ThrowsAsync<ConcurrentTimeEntryException>(
            () => new UnitOfWork(secondContext).SaveChangesAsync());

        await using var verify = new AppDbContext(options);
        var saved = (await new ReviewDecisionRepository(verify).GetForDecisionAsync(id))!;
        Assert.Equal(TimeEntryStatus.Approved, saved.GetCurrentAudit()!.Status);
        Assert.Equal(2, saved.Audits.Count);
        Assert.Single(saved.Reviews);
    }
}
