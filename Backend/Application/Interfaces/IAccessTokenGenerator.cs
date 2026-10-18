using Domain.Aggregate;

namespace Application.Interfaces;

public interface IAccessTokenGenerator
{
    AccessToken Generate(User user);
}

public sealed record AccessToken(string Value, DateTime ExpiresAt);
