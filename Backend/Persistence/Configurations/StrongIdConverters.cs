using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Persistence.Configurations;

public sealed class EmployeeIdConverter : ValueConverter<EmployeeId, Guid>
{
    public EmployeeIdConverter() : base(id => id.Value, value => EmployeeId.Create(value).Value) { }
}

public sealed class DepartmentIdConverter : ValueConverter<DepartmentId, Guid>
{
    public DepartmentIdConverter() : base(id => id.Value, value => DepartmentId.Create(value).Value) { }
}

public sealed class CatagoryIdConverter : ValueConverter<CatagoryId, Guid>
{
    public CatagoryIdConverter() : base(id => id.Value, value => CatagoryId.Create(value).Value) { }
}

public sealed class TaskIdConverter : ValueConverter<TaskId, Guid>
{
    public TaskIdConverter() : base(id => id.Value, value => TaskId.Create(value).Value) { }
}

public sealed class TimeEntryIdConverter : ValueConverter<TimeEntryId, Guid>
{
    public TimeEntryIdConverter() : base(id => id.Value, value => TimeEntryId.Create(value).Value) { }
}

public sealed class TimeEntryAuditIdConverter : ValueConverter<TimeEntryAuditId, Guid>
{
    public TimeEntryAuditIdConverter() : base(id => id.Value, value => TimeEntryAuditId.Create(value).Value) { }
}

public sealed class TimeEntryReviewIdConverter : ValueConverter<TimeEntryReviewId, Guid>
{
    public TimeEntryReviewIdConverter() : base(id => id.Value, value => TimeEntryReviewId.Create(value).Value) { }
}

public sealed class ExportIdConverter : ValueConverter<ExportId, Guid>
{
    public ExportIdConverter() : base(id => id.Value, value => ExportId.Create(value).Value) { }
}

public sealed class AccessPrivilegeIdConverter : ValueConverter<AccessPrivilegeId, Guid>
{
    public AccessPrivilegeIdConverter() : base(id => id.Value, value => AccessPrivilegeId.Create(value).Value) { }
}

public sealed class RegistrationPeriodModeIdConverter : ValueConverter<RegistrationPeriodModeId, Guid>
{
    public RegistrationPeriodModeIdConverter() : base(id => id.Value, value => RegistrationPeriodModeId.Create(value).Value) { }
}
