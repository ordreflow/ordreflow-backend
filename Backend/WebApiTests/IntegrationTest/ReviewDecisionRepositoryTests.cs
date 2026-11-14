using Domain.Aggregate;
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
    [Fact]
    public async Task Scope_uses_the_task_category_and_a_second_review_of_the_same_version_conflicts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var reviewer = Employee.Create("Reviewer", "reviewer@test.invalid", DateTime.UtcNow).Value;
        var worker = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        Assert.True(reviewer.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        var department = new Department
        {
            Id = DepartmentId.Create(Guid.NewGuid()).Value,
            Name = DepartmentName.Create("Work department").Value
        };
        var allowed = new Catagory
        {
            Id = CatagoryId.Create(Guid.NewGuid()).Value, DepartmentId = department.Id, CreatedBy = worker.Id,
            Name = CatagoryName.Create("Assigned").Value, Status = CatagoryStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var other = new Catagory
        {
            Id = CatagoryId.Create(Guid.NewGuid()).Value, DepartmentId = department.Id, CreatedBy = worker.Id,
            Name = CatagoryName.Create("Other").Value, Status = CatagoryStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var allowedTask = new Domain.Entities.Task
        {
            Id = TaskId.Create(Guid.NewGuid()).Value, CatagoryId = allowed.Id, Name = TaskName.Create("Allowed").Value,
            Status = TaskStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var otherTask = new Domain.Entities.Task
        {
            Id = TaskId.Create(Guid.NewGuid()).Value, CatagoryId = other.Id, Name = TaskName.Create("Other").Value,
            Status = TaskStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var initialTime = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var id = TimeEntryId.Create(Guid.NewGuid()).Value;
        var entry = TimeEntry.Create(worker.Id, TimeEntryAudit.Create(id, worker.Id, allowedTask.Id,
            WorkDate.Create(new DateOnly(2026, 10, 1)).Value, Duration.Create(2m).Value,
            TimeEntryDescription.Create("Work").Value, TimeEntryStatus.ReadyForApproval, initialTime).Value).Value;

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
