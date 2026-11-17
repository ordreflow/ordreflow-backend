using Application.Commands;
using Application.Handlers;
using Domain.ValueObjects;

namespace UnitTests.Features.FR13;

// FR-13/SEC-01: the displayed job title is separate from system access.
public class JobTitleTests
{
    [Fact]
    public void JobTitle_IsTrimmed_AndBlankMeansNoTitle()
    {
        Assert.Equal("Electrician", JobTitle.Create("  Electrician ").Value.Value);
        Assert.Null(JobTitle.CreateOptional("   ").Value);
        Assert.Null(JobTitle.CreateOptional(null).Value);
        Assert.Equal("JobTitleEmpty", Assert.Single(JobTitle.Create(" ").Errors).Code);
    }

    [Fact]
    public void JobTitleLongerThanLimit_IsRejected()
    {
        var result = JobTitle.CreateOptional(new string('a', JobTitle.MaxLength + 1));

        Assert.Equal("JobTitleTooLong", Assert.Single(result.Errors).Code);
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Administrator")]
    [InlineData("CEO")]
    public async Task ChangingJobTitle_DoesNotChangeSystemAccess(string title)
    {
        var scenario = new RoleChangeTestData.Scenario();

        var result = await new ChangeJobTitleHandler(scenario.Users).HandleAsync(
            new ChangeJobTitleCommand(scenario.Actor.UserId.Value, scenario.Target.UserId.Value, title));

        Assert.True(result.IsSuccess);
        Assert.Equal(title, scenario.Target.JobTitle!.Value);
        Assert.Equal(UserRole.Employee, scenario.Target.Role);
        Assert.False(scenario.Target.CanManageUsers);
        Assert.False(scenario.Target.CanReviewTimeEntries);
    }

    [Fact]
    public async Task ChangingSystemAccess_KeepsJobTitle()
    {
        var scenario = new RoleChangeTestData.Scenario();
        Assert.True(scenario.Target.ChangeJobTitle(UserRole.Admin, UserStatus.Active, JobTitle.Create("Electrician").Value)
            .IsSuccess);

        var result = await scenario.Handler().HandleAsync(scenario.Command("Manager"));

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Manager, scenario.Target.Role);
        Assert.Equal("Electrician", scenario.Target.JobTitle!.Value);
    }

    [Fact]
    public async Task Admin_ClearsJobTitle()
    {
        var scenario = new RoleChangeTestData.Scenario();
        Assert.True(scenario.Target.ChangeJobTitle(UserRole.Admin, UserStatus.Active, JobTitle.Create("Electrician").Value)
            .IsSuccess);

        var result = await new ChangeJobTitleHandler(scenario.Users).HandleAsync(
            new ChangeJobTitleCommand(scenario.Actor.UserId.Value, scenario.Target.UserId.Value, ""));

        Assert.True(result.IsSuccess);
        Assert.Null(scenario.Target.JobTitle);
    }

    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, false)]
    [InlineData(UserRole.Admin, true)]
    public async Task NonAdminOrInactiveAdmin_CannotChangeJobTitle(UserRole actorRole, bool inactive)
    {
        var scenario = new RoleChangeTestData.Scenario(actorRole);
        if (inactive)
            Assert.True(scenario.Actor.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);

        var result = await new ChangeJobTitleHandler(scenario.Users).HandleAsync(
            new ChangeJobTitleCommand(scenario.Actor.UserId.Value, scenario.Target.UserId.Value, "Manager"));

        Assert.Equal("JobTitleChangeForbidden", Assert.Single(result.Errors).Code);
        Assert.Null(scenario.Target.JobTitle);
    }

    [Fact]
    public async Task UnknownTarget_IsNotFound()
    {
        var scenario = new RoleChangeTestData.Scenario();

        var result = await new ChangeJobTitleHandler(scenario.Users).HandleAsync(
            new ChangeJobTitleCommand(scenario.Actor.UserId.Value, Guid.NewGuid(), "Electrician"));

        Assert.Equal("UserNotFound", Assert.Single(result.Errors).Code);
    }
}
