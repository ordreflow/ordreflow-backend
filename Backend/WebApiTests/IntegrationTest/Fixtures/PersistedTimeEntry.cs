using System.Reflection;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest.Fixtures;

// Read-side/corrupt-history fixtures simulate EF materialization. Production has no
// rehydration factory or generic audit-writing API. Normal domain tests use Record/Change/etc.
internal static class PersistedTimeEntry
{
    public static Result<TimeEntry> Create(EmployeeId employee, TimeEntryAudit initial)
    {
        var entry = (TimeEntry)Activator.CreateInstance(typeof(TimeEntry), nonPublic: true)!;
        typeof(TimeEntry).GetProperty(nameof(TimeEntry.Id))!.SetValue(entry, initial.TimeEntryId);
        typeof(TimeEntry).GetProperty(nameof(TimeEntry.EmployeeId))!.SetValue(entry, employee);
        Append(entry, initial);
        return Result<TimeEntry>.Success(entry);
    }

    public static Result Append(TimeEntry entry, TimeEntryAudit audit)
    {
        var audits = (List<TimeEntryAudit>)typeof(TimeEntry).GetField("_audits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(entry)!;
        audits.Add(audit);
        typeof(TimeEntry).GetProperty(nameof(TimeEntry.Revision))!.SetValue(entry, (long)audits.Count);
        return Result.Success();
    }
}
