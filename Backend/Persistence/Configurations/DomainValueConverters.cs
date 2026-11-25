using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Persistence.Configurations;

public sealed class PersonNameConverter : ValueConverter<PersonName, string>
{
    public PersonNameConverter() : base(value => value.Value, value => PersonName.Create(value).Value) { }
}

public sealed class EmailAddressConverter : ValueConverter<EmailAddress, string>
{
    public EmailAddressConverter() : base(value => value.Value, value => EmailAddress.Create(value).Value) { }
}

public sealed class CatagoryNameConverter : ValueConverter<CatagoryName, string>
{
    public CatagoryNameConverter() : base(value => value.Value, value => CatagoryName.Create(value).Value) { }
}

public sealed class TaskNameConverter : ValueConverter<TaskName, string>
{
    public TaskNameConverter() : base(value => value.Value, value => TaskName.Create(value).Value) { }
}

public sealed class DepartmentNameConverter : ValueConverter<DepartmentName, string>
{
    public DepartmentNameConverter() : base(value => value.Value, value => DepartmentName.Create(value).Value) { }
}

public sealed class TimeEntryDescriptionConverter : ValueConverter<TimeEntryDescription, string>
{
    public TimeEntryDescriptionConverter() : base(value => value.Value, value => TimeEntryDescription.Create(value).Value) { }
}

public sealed class RejectionReasonConverter : ValueConverter<RejectionReason, string>
{
    public RejectionReasonConverter() : base(value => value.Value, value => RejectionReason.Create(value).Value) { }
}

public sealed class WorkDateConverter : ValueConverter<WorkDate, DateOnly>
{
    public WorkDateConverter() : base(value => value.Value, value => WorkDate.Create(value).Value) { }
}

public sealed class TargetCompletionDateConverter : ValueConverter<TargetCompletionDate, DateOnly>
{
    public TargetCompletionDateConverter() : base(value => value.Value, value => TargetCompletionDate.Create(value).Value) { }
}

public sealed class PlannedDurationConverter : ValueConverter<PlannedDuration, decimal>
{
    public PlannedDurationConverter() : base(value => value.Value, value => PlannedDuration.Create(value).Value) { }
}

public sealed class CutoffDayConverter : ValueConverter<CutoffDay, int>
{
    public CutoffDayConverter() : base(value => value.Value, value => CutoffDay.Create(value).Value) { }
}
