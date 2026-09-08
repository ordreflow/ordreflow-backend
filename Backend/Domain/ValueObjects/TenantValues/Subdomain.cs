using System.Text.RegularExpressions;
using Core.Tools.OperationResult;

namespace Domain.ValueObjects;


    public record Subdomain
    {
        public string Value { get; }

        private Subdomain(string value)
        {
            Value = value;
        }

        public static Result<Subdomain> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result<Subdomain>.Failure(new Error("SubdomainCannotBeEmpty", "Subdomain cannot be empty."));
            }

            value = value.Trim().ToLowerInvariant();

            if (value.Length > 63)
            {
                return Result<Subdomain>.Failure(new Error("SubdomainTooLong", "Subdomain cannot exceed 63 characters."));
            }

            if (!Regex.IsMatch(
                    value,
                    @"^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$"))
            {
                return Result<Subdomain>.Failure(new Error("InvalidSubdomainFormat", "Subdomain format is invalid. Must start and end with a letter or digit, and can contain letters, digits, and hyphens."));
            }

            return Result<Subdomain>.Success(new Subdomain(value));
        }

        public override string ToString()
        {
            return Value;
        }
    }
