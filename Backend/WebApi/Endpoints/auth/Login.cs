using Application;
using Application.Commands;
using Application.Dtos;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.Auth;

namespace WebAPI.endpoints.Auth;

/// <summary>
/// Logs in with email and password and returns a JWT access token.
/// </summary>
public sealed class Login(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint.WithRequest<LoginRequest>.AndResponse<IResult>
{
    [AllowAnonymous]
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
