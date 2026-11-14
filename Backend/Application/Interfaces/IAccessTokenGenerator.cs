using Domain.Aggregate;

namespace Application.Interfaces;

public interface IAccessTokenGenerator
{
    AccessToken Generate(Employee employee);
}

public sealed record AccessToken(string Value, DateTime ExpiresAt);
