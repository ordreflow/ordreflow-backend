using Application;
using Application.Commands;
using Application.Dtos;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ObjectMapper;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Auth;

namespace WebAPI.endpoints.Auth;

/// <summary>
/// Logs in with email and password and returns a JWT access token.
/// Rate-limited per client IP; too many attempts return 429 with a structured error.
/// </summary>
public sealed class Login(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint.WithRequest<LoginRequest>.AndResponse<IResult>
{
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimiting.PolicyName)]
    [HttpPost("auth/login")]
    public override async Task<IResult> HandleAsync(LoginRequest request)
    {
        var result = await dispatcher.DispatchAsync<LoginCommand, Result<LoginDto>>(
            new LoginCommand(request.Email, request.Password));

        if (result.IsSuccess)
            return TypedResults.Ok(mapper.Map<LoginResponse>(result.Value));

        // Wrong email or password -> 401, valid login for an inactive user -> 403.
        var statusCode = result.Errors.Any(error => error.Code == "UserInactive")
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;

        return TypedResults.Json(result.Errors, statusCode: statusCode);
    }
}
