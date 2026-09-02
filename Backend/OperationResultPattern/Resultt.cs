namespace  Core.Tools.OperationResult;

public class Result<T> : Result
{
    private readonly T? _value;

    private Result( T value) : base(true, new List<Error>())
    {
        _value = value;
    }
    private Result(List<Error> errors) : base(false, errors)
    {
        _value = default;
    }
    
    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot access the value of a failed result.");

            return _value!;
        }
    }
    
    public static Result<T> Success(T value)
        => new(value);
    
    
    public static Result<T> Failure(params Error[] errors)
        => new(errors.ToList());

    public static Result Combine(params Result[] results)
    {
        var errors = results.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToList();
        return errors.Any() ? Failure(errors.ToArray()) : Success(default!);
    }
    
    public static implicit operator Result<T>(T value)
        => Success(value);


}