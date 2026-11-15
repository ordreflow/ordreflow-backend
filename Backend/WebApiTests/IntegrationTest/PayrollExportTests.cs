using System.Text;
using Application.Exports;
using Application.Commands;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Domain.Services;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;

namespace WebApiTests.IntegrationTest;

public sealed class PayrollExportTests
{
    private static readonly DateTime Created = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly From = new(2026, 11, 1);
    private static readonly DateOnly To = new(2026, 11, 30);
    private static readonly DateTime ExportedAt = Created.AddDays(30);

    [Theory]
    [InlineData(SystemPermission.Payroll)]
    [InlineData(SystemPermission.Administrator)]
    public async Task Creation_and_download_leave_entries_approved_and_save_a_fixed_csv(SystemPermission permission)
    {
        var actor = Actor(permission);
        var entry = Entry(actor.Id, From);
        var repository = new MemoryExports([Candidate(entry)]);
        var service = Service(actor, repository);
        var request = Request();

        var created = await service.CreateAsync(actor.Id.Value, request);
        var downloaded = await service.GetAsync(actor.Id.Value, request.ExportId);

        Assert.True(created.IsSuccess);
        Assert.True(downloaded.IsSuccess);
        Assert.Equal(TimeEntryStatus.Approved, entry.CurrentStatus);
        Assert.Null(created.Value.Export.TimeEntriesClosedAtUtc);
        Assert.Equal(2, entry.Revision); // creation + approval only; export adds no audit
        Assert.Equal(entry.Id.Value, Assert.Single(created.Value.Entries).TimeEntryId);
        Assert.Equal(entry.CurrentAudit!.Id.Value, created.Value.Entries[0].TimeEntryAuditId);
        Assert.Equal(7.5m, created.Value.Export.TotalHours);
        var content = downloaded.Value.CsvContent;
        content[0] = 0; // A caller cannot mutate the saved payload.
        Assert.Equal(Encoding.UTF8.GetPreamble()[0], downloaded.Value.CsvContent[0]);

        repository.Candidates.Clear(); // New live selection has no effect on an existing export.
        var again = await service.GetAsync(actor.Id.Value, request.ExportId);
        Assert.Equal(downloaded.Value.CsvContent, again.Value.CsvContent);
    }

    [Fact]
    public async Task Date_and_employee_filters_include_both_boundaries_and_exclude_other_statuses()
    {
        var actor = Actor(SystemPermission.Payroll);
        var other = EmployeeId.Create(Guid.NewGuid()).Value;
        var start = Entry(actor.Id, From);
        var end = Entry(actor.Id, To);
        var anotherEmployee = Entry(other, From);
        var ready = Entry(actor.Id, From, approved: false);
        var rejected = Entry(actor.Id, From, approved: false);
        Assert.True(rejected.Reject(other, "Wrong", Created.AddMinutes(1), false).IsSuccess);
        var closed = Entry(actor.Id, From);
        Assert.True(closed.CloseForExport(closed.CurrentAudit!.Id, actor.Id, Created.AddMinutes(2)).IsSuccess);
        var repository = new MemoryExports([Candidate(start), Candidate(end), Candidate(anotherEmployee),
            Candidate(ready), Candidate(rejected), Candidate(closed), Candidate(Entry(actor.Id, From.AddDays(-1))),
            Candidate(Entry(actor.Id, To.AddDays(1)))]);
        var service = Service(actor, repository);

        var all = await service.CreateAsync(actor.Id.Value, Request());
        var selected = await service.CreateAsync(actor.Id.Value, Request() with { EmployeeId = actor.Id.Value });

        Assert.Equal(3, all.Value.Entries.Count);
        Assert.Equal(new[] { start.Id.Value, end.Id.Value }.Order(), selected.Value.Entries.Select(item => item.TimeEntryId).Order());
        Assert.Equal(2, selected.Value.Entries.Count);
    }

    [Fact]
    public async Task A_single_day_is_valid_and_an_empty_selection_is_not_persisted()
    {
        var actor = Actor(SystemPermission.Payroll);
        var repository = new MemoryExports([Candidate(Entry(actor.Id, From))]);
        var service = Service(actor, repository);
        Assert.True((await service.CreateAsync(actor.Id.Value, Request() with { ToDate = From })).IsSuccess);

        var empty = await service.CreateAsync(actor.Id.Value, Request() with { FromDate = From.AddDays(1) });

        Assert.Equal("NoApprovedEntries", Assert.Single(empty.Errors).Code);
        Assert.Single(repository.Batches);
    }

    [Theory]
    [InlineData(SystemPermission.Employee, EmployeeStatus.Active)]
    [InlineData(SystemPermission.DepartmentManager, EmployeeStatus.Active)]
    [InlineData(SystemPermission.CatagoryReviewer, EmployeeStatus.Active)]
    [InlineData(SystemPermission.Payroll, EmployeeStatus.Deactivated)]
    public async Task Every_operation_requires_active_payroll_or_administrator_access(SystemPermission permission, EmployeeStatus status)
    {
        var actor = Actor(permission);
        if (status == EmployeeStatus.Deactivated) actor.Deactivate();
        var repository = new MemoryExports([]);
        var service = Service(actor, repository);
        var id = Guid.NewGuid();

        Result[] results = [await service.CreateAsync(actor.Id.Value, Request()), await service.GetAsync(actor.Id.Value, id),
            await service.ListAsync(actor.Id.Value, 1, 25), await service.EmployeesAsync(actor.Id.Value, null, 1, 25),
            await service.CloseTimeEntriesAsync(actor.Id.Value, id)];

        Assert.All(results, result => Assert.Equal("ExportForbidden", Assert.Single(result.Errors).Code));
        Assert.Equal(0, repository.SelectionReads);
    }

    [Fact]
    public async Task Retrying_a_create_reuses_the_original_batch_but_reusing_id_for_other_filters_conflicts()
    {
        var actor = Actor(SystemPermission.Payroll);
        var repository = new MemoryExports([Candidate(Entry(actor.Id, From))]);
        var service = Service(actor, repository);
        var request = Request();
        var first = await service.CreateAsync(actor.Id.Value, request);
        repository.Candidates.Clear();

        var retried = await service.CreateAsync(actor.Id.Value, request);
        var different = await service.CreateAsync(actor.Id.Value, request with { ToDate = To.AddDays(1) });

        Assert.Equal(first.Value.Export.Id, retried.Value.Export.Id);
        Assert.Equal(1, repository.SelectionReads);
        Assert.Single(repository.Batches);
        Assert.Equal("ExportRequestConflict", Assert.Single(different.Errors).Code);
    }

    [Fact]
    public async Task Invalid_filters_and_missing_batches_return_structured_errors_without_creating_a_batch()
    {
        var actor = Actor(SystemPermission.Payroll);
        var repository = new MemoryExports([]);
        var service = Service(actor, repository);
        var emptyId = await service.CreateAsync(actor.Id.Value, Request() with { ExportId = Guid.Empty });
        var missingFrom = await service.CreateAsync(actor.Id.Value, Request() with { FromDate = default });
        var backwards = await service.CreateAsync(actor.Id.Value, Request() with { ToDate = From.AddDays(-1) });
        var emptyEmployee = await service.CreateAsync(actor.Id.Value, Request() with { EmployeeId = Guid.Empty });
        var missing = await service.GetAsync(actor.Id.Value, Guid.NewGuid());

        Assert.Equal("ExportIdEmpty", Assert.Single(emptyId.Errors).Code);
        Assert.Equal("InvalidDateRange", Assert.Single(missingFrom.Errors).Code);
        Assert.Equal("InvalidDateRange", Assert.Single(backwards.Errors).Code);
        Assert.Equal("EmployeeIdEmpty", Assert.Single(emptyEmployee.Errors).Code);
        Assert.Equal("ExportNotFound", Assert.Single(missing.Errors).Code);
        Assert.Equal(0, repository.SelectionReads);
        Assert.Empty(repository.Batches);
    }

    [Fact]
    public void Overlapping_batches_close_entries_only_once_and_keep_original_files()
    {
        var actor = Actor(SystemPermission.Payroll);
        var entry = Entry(actor.Id, From);
        var first = Batch(actor.Id, entry);
        var second = Batch(actor.Id, entry);
        var original = first.CsvContent;
        var firstCloser = EmployeeId.Create(Guid.NewGuid()).Value;
        var secondCloser = EmployeeId.Create(Guid.NewGuid()).Value;

        Assert.Equal(TimeEntryStatus.Approved, entry.CurrentStatus);
        var closure = new ExportClosureDomainService();
        Assert.True(closure.Close(first, [entry], firstCloser, ExportedAt.AddMinutes(1)).IsSuccess);
        Assert.True(closure.Close(second, [entry], secondCloser, ExportedAt.AddMinutes(2)).IsSuccess);
        Assert.True(closure.Close(first, [entry], firstCloser, ExportedAt.AddMinutes(3)).IsSuccess);

        Assert.Equal(TimeEntryStatus.Closed, entry.CurrentStatus);
        Assert.Equal(3, entry.Revision);
        Assert.Equal(firstCloser, entry.CurrentAudit!.ClosedBy);
        Assert.Equal(firstCloser, first.TimeEntriesClosedBy);
        Assert.Equal(secondCloser, second.TimeEntriesClosedBy);
        Assert.Equal(ExportedAt.AddMinutes(1), first.TimeEntriesClosedAtUtc);
        Assert.Equal(2, first.Revision);
        Assert.Equal(original, first.CsvContent);
        Assert.Contains(",Approved,", Encoding.UTF8.GetString(first.CsvContent));
    }

    [Fact]
    public void One_conflicting_entry_prevents_the_entire_batch_from_closing()
    {
        var actor = Actor(SystemPermission.Payroll);
        var first = Entry(actor.Id, From);
        var second = Entry(actor.Id, From);
        var batch = Batch(actor.Id, first, second);
        var current = second.CurrentAudit!;
        Assert.True(PersistedTimeEntry.Append(second, TimeEntryAudit.Create(second.Id, actor.Id, current.TaskId, current.WorkDate,
            current.Duration, current.Description, TimeEntryStatus.Rejected, Created.AddMinutes(2)).Value).IsSuccess);

        var result = new ExportClosureDomainService().Close(batch, [first, second], actor.Id, ExportedAt.AddMinutes(1));

        Assert.Equal("ExportSelectionConflict", Assert.Single(result.Errors).Code);
        Assert.Equal(TimeEntryStatus.Approved, first.CurrentStatus);
        Assert.Null(batch.TimeEntriesClosedAtUtc);
        Assert.Equal(1, batch.Revision);
    }

    [Fact]
    public void Csv_preserves_danish_characters_quotes_multiline_text_and_neutralizes_formulas()
    {
        var owner = EmployeeId.Create(Guid.NewGuid()).Value;
        var entry = Entry(owner, From, comment: "Line \"one\",\nline two");
        var csv = ExportCsv.Create([new ExportCandidate(entry, "=FORMULA()", "Opsæt reoler", "København", "Løn")]);
        var text = Encoding.UTF8.GetString(csv[3..]);

        Assert.Equal(Encoding.UTF8.GetPreamble(), csv[..3]);
        Assert.StartsWith(ExportCsv.Header + "\r\n", text);
        Assert.Contains("2026-11-01,'=FORMULA(),Løn,København,Opsæt reoler,7.5,", text);
        Assert.Contains("\"Line \"\"one\"\",\nline two\"", text);
    }

    [Theory]
    [InlineData(TimeEntryStatus.Draft)]
    [InlineData(TimeEntryStatus.ReadyForApproval)]
    [InlineData(TimeEntryStatus.Rejected)]
    [InlineData(TimeEntryStatus.Closed)]
    [InlineData(TimeEntryStatus.Deleted)]
    public void Domain_refuses_to_export_an_ineligible_current_version(TimeEntryStatus status)
    {
        var actor = Actor(SystemPermission.Payroll);
        var entry = Entry(actor.Id, From);
        var current = entry.CurrentAudit!;
        Assert.True(PersistedTimeEntry.Append(entry, TimeEntryAudit.Create(entry.Id, actor.Id, current.TaskId, current.WorkDate,
            current.Duration, current.Description, status, Created.AddMinutes(2)).Value).IsSuccess);

        var result = Export.Create(ExportId.Create(Guid.NewGuid()).Value, actor.Id, From, To, null,
            [entry], [1], ExportedAt);

        Assert.Equal("ExportSelectionConflict", Assert.Single(result.Errors).Code);
    }

    internal static TimeEntry Entry(EmployeeId employee, DateOnly date, bool approved = true, string? comment = "Work")
    {
        var workDate = WorkDate.Create(date).Value;
        var entry = TimeEntry.Record(employee, TaskId.Create(Guid.NewGuid()).Value, workDate, Duration.Create(7.5m).Value,
            TimeEntryDescription.Create(comment).Value, WorkDatePeriod.Create(workDate, workDate).Value, Created).Value;
        if (approved) Assert.True(entry.Approve(EmployeeId.Create(Guid.NewGuid()).Value, Created.AddMinutes(1), true).IsSuccess);
        return entry;
    }

    private static Employee Actor(SystemPermission permission)
    {
        var actor = Employee.Create("Payroll", $"{Guid.NewGuid():N}@test.invalid", Created).Value;
        Assert.True(actor.GrantPermission(permission).IsSuccess);
        return actor;
    }
    private static CreateExportCommand Request() => new(Guid.Empty, Guid.NewGuid(), From, To);
    private static ExportCandidate Candidate(TimeEntry entry) => new(entry, "Worker", "Task", "Category", "Department");
    private static Export Batch(EmployeeId actor, params TimeEntry[] entries) => Export.Create(
        ExportId.Create(Guid.NewGuid()).Value, actor, From, To, null, entries,
        ExportCsv.Create(entries.Select(Candidate)), ExportedAt).Value;
    private static ExportTestHarness Service(Employee actor, MemoryExports repository) => new(new ActorRepository(actor), repository, new FixedClock());
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(ExportedAt);
    }
    private sealed class ActorRepository(Employee actor) : IEmployeeAccessRepository
    {
        public Task<Employee?> GetWithAccessesAsync(EmployeeId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Employee?>(actor.Id == id ? actor : null);
    }
    private sealed class MemoryExports(IEnumerable<ExportCandidate> entries) : IExportRepository
    {
        public List<ExportCandidate> Candidates { get; } = entries.ToList();
        public Dictionary<ExportId, Export> Batches { get; } = [];
        public int SelectionReads { get; private set; }
        public Task<bool> EmployeeExistsAsync(EmployeeId employee, CancellationToken ct) => Task.FromResult(true);
        public Task<IReadOnlyList<ExportCandidate>> SelectApprovedAsync(DateOnly from, DateOnly to, EmployeeId? employee, CancellationToken ct)
        {
            SelectionReads++;
            return Task.FromResult<IReadOnlyList<ExportCandidate>>(Candidates.Where(item =>
                item.Entry.CurrentStatus == TimeEntryStatus.Approved && item.Entry.CurrentWorkDate!.Value >= from &&
                item.Entry.CurrentWorkDate.Value <= to && (employee is null || item.Entry.EmployeeId == employee)).ToArray());
        }
        public Task<Export?> GetAsync(ExportId id, CancellationToken ct) => Task.FromResult(Batches.GetValueOrDefault(id));
        public Task AddAsync(Export batch, CancellationToken ct)
        {
            Batches.Add(batch.Id, batch);
            return Task.CompletedTask;
        }
        public Task<Export?> GetForClosureAsync(ExportId id, CancellationToken ct) => GetAsync(id, ct);
        public Task<IReadOnlyList<TimeEntry>> GetEntriesForClosureAsync(IReadOnlyCollection<TimeEntryId> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<TimeEntry>>(Candidates.Select(item => item.Entry).Where(item => ids.Contains(item.Id)).ToArray());
        public Task<IReadOnlyList<ExportSummaryDto>> ListAsync(int page, int size, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<ExportEmployeeDto>> EmployeesAsync(string? search, int page, int size, CancellationToken ct) => throw new NotSupportedException();
    }
}
