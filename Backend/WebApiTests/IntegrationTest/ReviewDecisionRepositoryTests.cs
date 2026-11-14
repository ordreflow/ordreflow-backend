using Domain.Aggregate;
using Domain.Entities;
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
    [Fact]
    public async Task Scope_uses_the_task_category_and_a_second_review_of_the_same_version_conflicts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var reviewer = Employee.Create("Reviewer", "reviewer@test.invalid", DateTime.UtcNow).Value;
        var worker = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        Assert.True(reviewer.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        var department = new Department { Id = Guid.NewGuid(), Name = "Work department" };
        var allowed = new Catagory
        {
            Id = Guid.NewGuid(), DepartmentId = department.Id, CreatedBy = worker.Id,
            Name = "Assigned", Status = CatagoryStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var other = new Catagory
        {
            Id = Guid.NewGuid(), DepartmentId = department.Id, CreatedBy = worker.Id,
            Name = "Other", Status = CatagoryStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var allowedTask = new Domain.Entities.Task
        {
            Id = Guid.NewGuid(), CatagoryId = allowed.Id, Name = "Allowed",
            Status = TaskStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var otherTask = new Domain.Entities.Task
        {
            Id = Guid.NewGuid(), CatagoryId = other.Id, Name = "Other",
            Status = TaskStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var initialTime = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        var entry = TimeEntry.Create(worker.Id, TimeEntryAudit.Create(id, worker.Id, allowedTask.Id,
            new DateOnly(2026, 10, 1), 2, "Work", TimeEntryStatus.ReadyForApproval, initialTime).Value).Value;

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
        Assert.True(second.Reject(reviewer.Id, "Incorrect hours", initialTime.AddMinutes(2), allowSelfReview: true).IsSuccess);

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
