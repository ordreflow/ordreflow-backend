using System.Security.Claims;

namespace WebAPI.Common;

public static class CurrentUserExtensions
{
    public static bool TryGetCurrentUserId(
        this HttpContext httpContext,
        out Guid userId)
    {
        userId = Guid.Empty;
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            return false;

        userId = id;
        return true;
    }
}
