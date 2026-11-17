using Application.Dtos;
using ObjectMapper;
using WebAPI.Contracts.Auth;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.MappingConfigurations;

public sealed class TimeEntryToResponseMapper
    : IMappingConfig<TimeEntryDto, TimeEntryResponse>
{
    public TimeEntryResponse Map(TimeEntryDto input) => new(
        input.Id,
        input.TaskId,
        input.Date,
        input.Hours,
        input.Comment,
        input.EmployeeId,
        input.Status,
        input.RejectionReason);
}

public sealed class TimeEntryReviewToResponseMapper
    : IMappingConfig<TimeEntryReviewDto, TimeEntryReviewResponse>
{
    public TimeEntryReviewResponse Map(TimeEntryReviewDto input) => new(
        input.Id,
        input.TimeEntryId,
        input.Decision,
        input.Reason,
        input.ReviewedAt);
}

public sealed class LoginToResponseMapper
    : IMappingConfig<LoginDto, LoginResponse>
{
    public LoginResponse Map(LoginDto input) => new(
        input.AccessToken,
        "Bearer",
        input.ExpiresAt);
}
