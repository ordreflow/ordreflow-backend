using Application.Commands;
using Application.Dtos;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

// Dispatched through IQueryDispatcher because login returns a token,
// while ICommandDispatcher only returns a plain Result.
public sealed class LoginHandler(
    IPasswordVerifier passwordVerifier,
    IEmployeeAccessRepository employees,
    IAccessTokenGenerator tokenGenerator) : IQueryHandler<
        LoginCommand,
        Result<LoginDto>>
{
    private static readonly Error InvalidCredentials =
        new("InvalidCredentials", "The email or password is incorrect.");

    public async Task<Result<LoginDto>> HandleAsync(LoginCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
            return Result<LoginDto>.Failure(InvalidCredentials);

        var employeeId = await passwordVerifier.VerifyAsync(command.Email.Trim(), command.Password);
        if (employeeId is null)
            return Result<LoginDto>.Failure(InvalidCredentials);

        var employee = await employees.GetWithAccessesAsync(EmployeeId.Create(employeeId.Value).Value);
        if (employee is null)
            return Result<LoginDto>.Failure(InvalidCredentials);

        // Checked after the password, so the status is not revealed to someone without valid credentials.
        if (employee.Status != EmployeeStatus.Active)
            return Result<LoginDto>.Failure(new Error("UserInactive", "The user is inactive."));
        if (!employee.HasPermission(SystemPermission.Employee))
            return Result<LoginDto>.Failure(new Error("AccessForbidden", "Employee access is required."));

        var token = tokenGenerator.Generate(employee);
        return Result<LoginDto>.Success(new LoginDto(token.Value, token.ExpiresAt));
    }
}
