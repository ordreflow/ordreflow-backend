using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI.Authentication;

// A class rather than a record, so the secret never shows up in a generated ToString().
public sealed class JwtOptions
{
    private JwtOptions(string secret, string issuer, string audience, int expiresMinutes)
    {
        SigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        Issuer = issuer;
        Audience = audience;
        ExpiresMinutes = expiresMinutes;
    }

    public SymmetricSecurityKey SigningKey { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public int ExpiresMinutes { get; }

    public static JwtOptions FromEnvironment()
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("JWT_SECRET must be set and be at least 32 bytes long.");

        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        var expiresMinutes =
            int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRES_MINUTES"), out var minutes) && minutes > 0
                ? minutes
                : 60;

        return new JwtOptions(
            secret,
            string.IsNullOrWhiteSpace(issuer) ? "ordreflow-api" : issuer,
            string.IsNullOrWhiteSpace(audience) ? "ordreflow-frontend" : audience,
            expiresMinutes);
    }
}
