using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TaskName
{
    public string Value { get; }
    private TaskName(string value) => Value = value;

    public static Result<TaskName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<TaskName>.Failure(new Error("TaskNameRequired", "Task name is required."));
        value = value.Trim();
        return value.Length > 200
            ? Result<TaskName>.Failure(new Error("TaskNameTooLong", "Task name cannot exceed 200 characters."))
            : Result<TaskName>.Success(new TaskName(value));
    }

    public override string ToString() => Value;
}
