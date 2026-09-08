using System.Net.Mail;
using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record EmailAddress
{
    public string Value { get; }

    private EmailAddress(string value) => Value = value;

    public static Result<EmailAddress> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<EmailAddress>.Failure(new Error("EmailEmpty", "Email address cannot be empty."));

        value = value.Trim().ToLowerInvariant();
        try
        {
            var address = new MailAddress(value);
            if (!string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase))
                return Result<EmailAddress>.Failure(new Error("EmailInvalid", "Email address is invalid."));
        }
        catch (FormatException)
        {
            return Result<EmailAddress>.Failure(new Error("EmailInvalid", "Email address is invalid."));
        }

        return value.Length > 320
            ? Result<EmailAddress>.Failure(new Error("EmailTooLong", "Email address cannot exceed 320 characters."))
            : Result<EmailAddress>.Success(new EmailAddress(value));
    }

    public override string ToString() => Value;
}