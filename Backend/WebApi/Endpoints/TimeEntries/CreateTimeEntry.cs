using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

public class CreateTimeEntry(
    ICommandDispatcher Dispatcher,
    IMapper Mapper)
    : ApiEndpoint
        .WithRequest<CreateTimeEntryRequest>
        .AndResponse<
            Results<
                Ok<TimeEntryResponse>,
                BadRequest<IEnumerable<Error>>>>
{
    [HttpPost("time_entries")]
    public override async Task<
            Results<
                Ok<TimeEntryResponse>,
                BadRequest<IEnumerable<Error>>>>
        HandleAsync(CreateTimeEntryRequest request)
    {
        // TODO: Authentication
        // Resolve the authenticated UserId in the Application handler. It must
        // not be accepted from CreateTimeEntryRequest.

        // TODO: Application/Domain/Persistence
        // CreateTimeEntryCommand is currently empty and its handler is not
        // implemented. The handler must create the TimeEntry, validate it
        // through the Domain, attach it to the current user's TimeSheet, and
        // persist it through the repository/UnitOfWork.

        // TODO: Mapping
        // Replace the current commented mapping configuration with explicit
        // request-to-command and command/result-to-TimeEntryResponse mappings.

        var command =
            Mapper.Map<CreateTimeEntryCommand>(request);

        var dispatchResult =
            await Dispatcher.DispatchAsync(command);

        if (dispatchResult.IsSuccess)
        {
            // TODO: API contract
            // Return 201 Created with the resource location once the command
            // returns the generated entry ID. The current dispatcher only
            // returns Result, so the Application result flow needs to expose
            // the created value first.
            var response =
                Mapper.Map<TimeEntryResponse>(command);

            return TypedResults.Ok(response);
        }

        return TypedResults.BadRequest(
            (IEnumerable<Error>)dispatchResult.Errors);
    }
}
