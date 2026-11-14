namespace Domain.Interfaces.IUnitOfWork;

public sealed class ConcurrentTimeEntryException : Exception
{
    public ConcurrentTimeEntryException()
        : base("The time entry changed during this operation.") { }

    public ConcurrentTimeEntryException(Exception innerException)
        : base("The time entry changed during this operation.", innerException) { }
}
