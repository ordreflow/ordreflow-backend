namespace Application;

public interface ITransactionalCommand
{
    string ConflictCode { get; }
}
