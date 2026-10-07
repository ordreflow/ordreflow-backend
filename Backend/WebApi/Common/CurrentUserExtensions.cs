using System.Security.Claims;
using Domain.ValueObjects;

namespace WebAPI.Common;

public static class CurrentUserExtensions
{
    public static bool TryGetCurrentUserId(
        this HttpContext httpContext,
        out UserId userId)
    {
        userId = null!;
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        return Guid.TryParse(value, out var id) &&
            UserId.Create(id).IsSuccess &&
            (userId = UserId.Create(id).Value) is not null;
    }
}
