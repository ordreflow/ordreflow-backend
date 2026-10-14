using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;

namespace WebAPI.Common;

/// <summary>
/// Maps a failed <see cref="Result"/>/<see cref="Result{T}"/> to an appropriate
/// HTTP status code based on error code naming conventions:
/// "*NotFound" -> 404, "*Forbidden" -> 403, everything else -> 400.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToErrorResult(this IReadOnlyList<Error> errors)
    {
        if (errors.Any(error => error.Code.EndsWith("NotFound", StringComparison.Ordinal)))
            return TypedResults.NotFound(errors);

        if (errors.Any(error => error.Code.EndsWith("Forbidden", StringComparison.Ordinal)))
            return TypedResults.Json(errors, statusCode: StatusCodes.Status403Forbidden);

        return TypedResults.BadRequest(errors);
    }
}
