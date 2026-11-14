using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record RegistrationPeriodModeId
{
    public Guid Value { get; }
    private RegistrationPeriodModeId(Guid value) => Value = value;

    public static Result<RegistrationPeriodModeId> Create(Guid value) => value == Guid.Empty
        ? Result<RegistrationPeriodModeId>.Failure(new Error("RegistrationPeriodModeIdEmpty", "Registration period mode ID cannot be empty."))
        : Result<RegistrationPeriodModeId>.Success(new RegistrationPeriodModeId(value));

    public override string ToString() => Value.ToString();
}
