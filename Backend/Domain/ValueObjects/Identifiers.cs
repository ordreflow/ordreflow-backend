using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public abstract record GuidIdentifier
{
    protected GuidIdentifier(Guid value) => Value = value;

    public Guid Value { get; }

    protected static Result<TIdentifier> CreateIdentifier<TIdentifier>(
        Guid value,
        Func<Guid, TIdentifier> factory,
        string name)
        where TIdentifier : GuidIdentifier
    {
        if (value == Guid.Empty)
            return Result<TIdentifier>.Failure(
                new Error($"{name}Required", $"A valid {name} is required."));

        return Result<TIdentifier>.Success(factory(value));
    }

    protected static TIdentifier NewIdentifier<TIdentifier>(Func<Guid, TIdentifier> factory) =>
        factory(Guid.NewGuid());
}

public sealed record EmployeeId : GuidIdentifier
{
    private EmployeeId(Guid value) : base(value) { }
    public static Result<EmployeeId> Create(Guid value) => CreateIdentifier(value, id => new EmployeeId(id), "EmployeeId");
    public static EmployeeId New() => NewIdentifier(id => new EmployeeId(id));
}

public sealed record DepartmentId : GuidIdentifier
{
    private DepartmentId(Guid value) : base(value) { }
    public static Result<DepartmentId> Create(Guid value) => CreateIdentifier(value, id => new DepartmentId(id), "DepartmentId");
    public static DepartmentId New() => NewIdentifier(id => new DepartmentId(id));
}

public sealed record CatagoryId : GuidIdentifier
{
    private CatagoryId(Guid value) : base(value) { }
    public static Result<CatagoryId> Create(Guid value) => CreateIdentifier(value, id => new CatagoryId(id), "CatagoryId");
    public static CatagoryId New() => NewIdentifier(id => new CatagoryId(id));
}

public sealed record TaskId : GuidIdentifier
{
    private TaskId(Guid value) : base(value) { }
    public static Result<TaskId> Create(Guid value) => CreateIdentifier(value, id => new TaskId(id), "TaskId");
    public static TaskId New() => NewIdentifier(id => new TaskId(id));
}

public sealed record TimeEntryId : GuidIdentifier
{
    private TimeEntryId(Guid value) : base(value) { }
    public static Result<TimeEntryId> Create(Guid value) => CreateIdentifier(value, id => new TimeEntryId(id), "TimeEntryId");
    public static TimeEntryId New() => NewIdentifier(id => new TimeEntryId(id));
}

public sealed record TimeEntryAuditId : GuidIdentifier
{
    private TimeEntryAuditId(Guid value) : base(value) { }
    public static Result<TimeEntryAuditId> Create(Guid value) => CreateIdentifier(value, id => new TimeEntryAuditId(id), "TimeEntryAuditId");
    public static TimeEntryAuditId New() => NewIdentifier(id => new TimeEntryAuditId(id));
}

public sealed record TimeEntryReviewId : GuidIdentifier
{
    private TimeEntryReviewId(Guid value) : base(value) { }
    public static Result<TimeEntryReviewId> Create(Guid value) => CreateIdentifier(value, id => new TimeEntryReviewId(id), "TimeEntryReviewId");
    public static TimeEntryReviewId New() => NewIdentifier(id => new TimeEntryReviewId(id));
}

public sealed record RegistrationPeriodModeId : GuidIdentifier
{
    private RegistrationPeriodModeId(Guid value) : base(value) { }
    public static Result<RegistrationPeriodModeId> Create(Guid value) => CreateIdentifier(value, id => new RegistrationPeriodModeId(id), "RegistrationPeriodModeId");
    public static RegistrationPeriodModeId New() => NewIdentifier(id => new RegistrationPeriodModeId(id));
}

public sealed record AccessPrivilegeId : GuidIdentifier
{
    private AccessPrivilegeId(Guid value) : base(value) { }
    public static Result<AccessPrivilegeId> Create(Guid value) => CreateIdentifier(value, id => new AccessPrivilegeId(id), "AccessPrivilegeId");
    public static AccessPrivilegeId New() => NewIdentifier(id => new AccessPrivilegeId(id));
}

public sealed record ExportId : GuidIdentifier
{
    private ExportId(Guid value) : base(value) { }
    public static Result<ExportId> Create(Guid value) => CreateIdentifier(value, id => new ExportId(id), "ExportId");
    public static ExportId New() => NewIdentifier(id => new ExportId(id));
}
