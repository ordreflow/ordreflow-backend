namespace Core.Tools.OperationResult;

public sealed record Error(
    string Code,
    string Message,
    string? Type = null
    );
