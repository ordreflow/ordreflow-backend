using System.Security.Claims;
using Application.Interfaces;
using Domain.Aggregate;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI.Authentication;

public sealed class JwtTokenGenerator(JwtOptions options) : IAccessTokenGenerator
{
    public AccessToken Generate(Employee employee)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(options.ExpiresMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, employee.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            SigningCredentials = new SigningCredentials(options.SigningKey, SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
