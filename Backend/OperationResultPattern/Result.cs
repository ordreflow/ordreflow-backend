namespace  Core.Tools.OperationResult;

public class Result
{
    protected Result(bool isSuccess, List<Error> errors)
    {
        if (isSuccess && errors.Any())
            throw new InvalidOperationException();

        if (!isSuccess && !errors.Any())
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<Error> Errors { get; }

    public static Result Success()
        => new(true, new List<Error>());

    public static Result Failure(params Error[] errors)
        => new(false, errors.ToList());
    
    public static Result Combine(params Result[] results)
    {
        var errors = results.Where(r => r.IsFailure).SelectMany(r => r.Errors).ToList();
        return errors.Any() ? Failure(errors.ToArray()) : Success();
    }
}