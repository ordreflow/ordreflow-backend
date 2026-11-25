using System.Net.Mail;
using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; }
    private EmailAddress(string value) => Value = value;

    public static Result<EmailAddress> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<EmailAddress>.Failure(new Error("EmailEmpty", "Email address is required."));
        value = value.Trim().ToLowerInvariant();
        if (value.Length > 320 || !MailAddress.TryCreate(value, out var email) ||
            !string.Equals(email.Address, value, StringComparison.OrdinalIgnoreCase))
            return Result<EmailAddress>.Failure(new Error("EmailInvalid", "Email address is invalid."));
        return Result<EmailAddress>.Success(new EmailAddress(value));
    }

    public override string ToString() => Value;
}
