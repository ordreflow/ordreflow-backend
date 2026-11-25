using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class ValueObjectTests
{
    [Fact]
    public void Typed_ids_reject_empty_guids_and_compare_by_value()
    {
        var id = Guid.NewGuid();
        Assert.Equal("EmployeeIdEmpty", Assert.Single(EmployeeId.Create(Guid.Empty).Errors).Code);
        Assert.Equal("TimeEntryIdEmpty", Assert.Single(TimeEntryId.Create(Guid.Empty).Errors).Code);
        Assert.Equal("TaskIdEmpty", Assert.Single(TaskId.Create(Guid.Empty).Errors).Code);
        Assert.Equal(EmployeeId.Create(id).Value, EmployeeId.Create(id).Value);
    }

    [Fact]
    public void Names_email_and_description_own_their_normalization_rules()
    {
        Assert.Equal("Jane Doe", PersonName.Create(" Jane Doe ").Value.Value);
        Assert.Equal("jane@example.com", EmailAddress.Create(" Jane@Example.com ").Value.Value);
        Assert.Equal("Project", CatagoryName.Create(" Project ").Value.Value);
        Assert.Equal("Task", TaskName.Create(" Task ").Value.Value);
        Assert.Equal(string.Empty, TimeEntryDescription.Create(null).Value.Value);
        Assert.Equal("DescriptionTooLong", Assert.Single(
            TimeEntryDescription.Create(new string('x', 1001)).Errors).Code);
        Assert.Equal("EmailInvalid", Assert.Single(EmailAddress.Create("invalid").Errors).Code);
    }

    [Fact]
    public void Reason_date_and_planned_duration_own_their_intrinsic_rules()
    {
        Assert.Equal("Fix hours", RejectionReason.Create(" Fix hours ").Value.Value);
        Assert.Equal("RejectReasonRequired", Assert.Single(RejectionReason.Create(" ").Errors).Code);
        Assert.Equal("WorkDateRequired", Assert.Single(WorkDate.Create(default).Errors).Code);
        Assert.Equal("InvalidDayInMonth", Assert.Single(CutoffDay.Create(29).Errors).Code);
        Assert.Equal("InvalidPlannedDuration", Assert.Single(PlannedDuration.Create(0m).Errors).Code);
        Assert.True(PlannedDuration.Create(40m).IsSuccess);
        Assert.Equal("TargetDateRequired", Assert.Single(TargetCompletionDate.Create(default).Errors).Code);
    }
}
