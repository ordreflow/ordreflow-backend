using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Persistence;

internal sealed class EmployeeIdValueConverter : ValueConverter<EmployeeId, Guid>
{
    public EmployeeIdValueConverter() : base(id => id.Value, value => EmployeeId.Create(value).Value) { }
}

internal sealed class DepartmentIdValueConverter : ValueConverter<DepartmentId, Guid>
{
    public DepartmentIdValueConverter() : base(id => id.Value, value => DepartmentId.Create(value).Value) { }
}

internal sealed class CatagoryIdValueConverter : ValueConverter<CatagoryId, Guid>
{
    public CatagoryIdValueConverter() : base(id => id.Value, value => CatagoryId.Create(value).Value) { }
}

internal sealed class TaskIdValueConverter : ValueConverter<TaskId, Guid>
{
    public TaskIdValueConverter() : base(id => id.Value, value => TaskId.Create(value).Value) { }
}

internal sealed class TimeEntryIdValueConverter : ValueConverter<TimeEntryId, Guid>
{
    public TimeEntryIdValueConverter() : base(id => id.Value, value => TimeEntryId.Create(value).Value) { }
}

internal sealed class TimeEntryAuditIdValueConverter : ValueConverter<TimeEntryAuditId, Guid>
{
    public TimeEntryAuditIdValueConverter() : base(id => id.Value, value => TimeEntryAuditId.Create(value).Value) { }
}

internal sealed class TimeEntryReviewIdValueConverter : ValueConverter<TimeEntryReviewId, Guid>
{
    public TimeEntryReviewIdValueConverter() : base(id => id.Value, value => TimeEntryReviewId.Create(value).Value) { }
}

internal sealed class RegistrationPeriodModeIdValueConverter : ValueConverter<RegistrationPeriodModeId, Guid>
{
    public RegistrationPeriodModeIdValueConverter() : base(id => id.Value, value => RegistrationPeriodModeId.Create(value).Value) { }
}

internal sealed class AccessPrivilegeIdValueConverter : ValueConverter<AccessPrivilegeId, Guid>
{
    public AccessPrivilegeIdValueConverter() : base(id => id.Value, value => AccessPrivilegeId.Create(value).Value) { }
}

internal sealed class ExportIdValueConverter : ValueConverter<ExportId, Guid>
{
    public ExportIdValueConverter() : base(id => id.Value, value => ExportId.Create(value).Value) { }
}

internal sealed class WorkDateValueConverter : ValueConverter<WorkDate, DateOnly>
{
    public WorkDateValueConverter() : base(date => date.Value, value => WorkDate.Create(value).Value) { }
}
