using Application.Dtos;
using Application.Queries;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace UnitTests.Features.FR04;

public class EmployeeWeekEntriesProjectionTests
{
    // Unsorted pre-scoped entries are returned in date order with all DTO fields intact.
    [Fact]
    public async Task UnsortedEntries_AreSortedAndProjectedWithAllFields()
    {
        var employeeId = EmployeeWeekEntriesTestData.CreateEmployeeId();
        var later = EmployeeWeekEntriesTestData.CreateEntry(employeeId,
            new DateTime(2026, 9, 11, 8, 0, 0), 9m, null,
            Guid.Parse("9f58cc1d-e5dc-40b2-8106-1a1f4b486e61"));
        var earlier = EmployeeWeekEntriesTestData.CreateEntry(employeeId,
            new DateTime(2026, 9, 9, 13, 45, 0), 1.25m, "Earlier work",
            Guid.Parse("4d94dd57-40b3-41f0-9e49-9a5e3513b8a9"));
        var managerId = UserId.Create(Guid.Parse("3644df08-8793-4baa-916b-1c842279b665")).Value;
        Assert.True(later.Accept(managerId, employeeId, managerId,
            UserRole.Manager, UserStatus.Active).IsSuccess);
        var (handler, _) = EmployeeWeekEntriesTestData.CreateHandler(later, earlier);

        var result = await handler.HandleAsync(new GetMyTimeEntriesForWeekQuery(
            employeeId.Value, new DateTime(2026, 9, 7), new DateTime(2026, 9, 14)));

        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value,
            dto => AssertDto(earlier, dto, "Draft"),
            dto => AssertDto(later, dto, "Accepted"));
    }

    private static void AssertDto(TimeEntry entry, TimeEntryDto dto, string expectedStatus)
    {
        Assert.Equal(entry.Id.Value, dto.Id);
        Assert.Equal(entry.TaskId.Value, dto.TaskId);
        Assert.Equal(entry.Date, dto.Date);
        Assert.Equal(entry.Hours, dto.Hours);
        Assert.Equal(entry.Comment, dto.Comment);
        Assert.Equal(entry.EmployeeId.Value, dto.EmployeeId);
        Assert.Equal(expectedStatus, dto.Status);
    }
}
