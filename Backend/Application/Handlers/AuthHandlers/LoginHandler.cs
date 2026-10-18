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
    IUserRepository userRepository,
    IAccessTokenGenerator tokenGenerator) : IQueryHandler<
        LoginCommand,
        Result<LoginDto>>
{
    private static readonly Error InvalidCredentials =
        new("InvalidCredentials", "The email or password is incorrect.");

    public async Task<Result<LoginDto>> HandleAsync(LoginCommand command)
    {
        var email = EmailAddress.Create(command.Email);
        if (email.IsFailure || string.IsNullOrEmpty(command.Password))
            return Result<LoginDto>.Failure(InvalidCredentials);

        var userId = await passwordVerifier.VerifyAsync(email.Value, command.Password);
        if (userId is null)
            return Result<LoginDto>.Failure(InvalidCredentials);

        var user = await userRepository.GetAsync(userId);
        if (user is null)
            return Result<LoginDto>.Failure(InvalidCredentials);

        // Checked after the password, so the status is not revealed to someone without valid credentials.
        if (user.Status != UserStatus.Active)
            return Result<LoginDto>.Failure(new Error("UserInactive", "The user is inactive."));

        var token = tokenGenerator.Generate(user);
        return Result<LoginDto>.Success(new LoginDto(token.Value, token.ExpiresAt));
    }
}
