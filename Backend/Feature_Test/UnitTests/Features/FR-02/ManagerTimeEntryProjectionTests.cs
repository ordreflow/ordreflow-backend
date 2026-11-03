using Application.Dtos;
using Application.Handlers;
using Application.Queries;
using Domain.Aggregate;

namespace UnitTests.Features.FR02;

public class ManagerTimeEntryProjectionTests
{
    // Unsorted pre-scoped entries are returned by date with every DTO field intact.
    [Fact]
    public async Task UnsortedEntries_SortByDateAndProjectAllFields()
    {
        var manager = ManagerTimeEntryTestData.CreateManager();
        var firstEmployee = ManagerTimeEntryTestData.CreateAssignedEmployee(manager);
        var secondEmployee = ManagerTimeEntryTestData.CreateAssignedEmployee(manager, "second@example.com");
        var later = ManagerTimeEntryTestData.CreateEntry(secondEmployee,
            new DateTime(2026, 9, 11, 8, 0, 0, DateTimeKind.Utc), 9m, null);
        var earlier = ManagerTimeEntryTestData.CreateEntry(firstEmployee,
            new DateTime(2026, 9, 9, 13, 45, 0, DateTimeKind.Utc), 1.25m, "Earlier work");
        Assert.True(earlier.Return(manager.UserId, firstEmployee.UserId, firstEmployee.ManagerId,
            manager.Role, manager.Status, "Needs correction").IsSuccess);
        var entries = new FakeTimeEntryRepository(later, earlier);
        var handler = new GetManagerTimeEntriesHandler(entries, new FakeUserRepository(manager));

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(manager.UserId.Value, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value,
            dto => AssertDto(earlier, dto, "Returned"),
            dto => AssertDto(later, dto, "Draft"));
    }

    private static void AssertDto(TimeEntry entry, TimeEntryDto dto, string status)
    {
        Assert.Equal(entry.Id.Value, dto.Id);
        Assert.Equal(entry.TaskId.Value, dto.TaskId);
        Assert.Equal(entry.Date, dto.Date);
        Assert.Equal(entry.Hours, dto.Hours);
        Assert.Equal(entry.Comment, dto.Comment);
        Assert.Equal(entry.EmployeeId.Value, dto.EmployeeId);
        Assert.Equal(status, dto.Status);
    }
}
