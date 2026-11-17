using System.Threading.RateLimiting;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace WebAPI.Authentication;

/// <summary>
/// How many login attempts (successful or not) one client IP may make per window (FR-06, SEC-04).
/// Colleagues often share one public IP, so this is set for slowing down attacks rather than single users;
/// Identity's account lockout limits repeated failures for a single account on top of this.
/// Override with the environment variables LoginRateLimit__PermitLimit and LoginRateLimit__Window.
/// </summary>
public sealed class LoginRateLimitOptions
{
    public const string SectionName = "LoginRateLimit";

    public int PermitLimit { get; set; } = 20;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

public static class LoginRateLimiting
{
    public const string PolicyName = "login";

    public static readonly Error TooManyAttempts =
        new("TooManyLoginAttempts", "Too many login attempts. Wait a moment and try again.");

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<LoginRateLimitOptions>().BindConfiguration(LoginRateLimitOptions.SectionName);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partitioned by client IP. Behind the frontend's nginx (and Caddy in test) the IP comes
            // from X-Forwarded-For through the forwarded headers middleware.
            options.AddPolicy(PolicyName, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<IOptions<LoginRateLimitOptions>>().Value;
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = settings.Window,
                    QueueLimit = 0
                });
            });

            // Same structured error list as other API failures, so the frontend can show it (INT-01).
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(new[] { TooManyAttempts }, cancellationToken);
            };
        });

        return services;
    }
}
