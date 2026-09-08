using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Handlers;

public class CreateTimeEntryHandler : ICommandHandler<CreateTimeEntryCommand>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public CreateTimeEntryHandler(
        ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public Task<Result> HandleAsync(CreateTimeEntryCommand command)
    {
        throw new NotImplementedException();
    }
}