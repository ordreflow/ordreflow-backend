using Application.Commands;
using Backend.TimeEntries;
//use namespace Backend.WebApi;

namespace WebAPI.MappingConfigurations;
using ObjectMapper;



public class CreateTimeEntryRequestToCommandMapper
    : IMappingConfig<CreateTimeEntry.CreateTimeEntryRequest, CreateTimeEntryCommand>
{
    public CreateTimeEntryCommand Map(CreateTimeEntry.CreateTimeEntryRequest input)
    {
        return new CreateTimeEntryCommand(
            input.RequestBody.WorkItemId,
            input.RequestBody.Date,
            input.RequestBody.Hours,
            input.RequestBody.StartTime,
            input.RequestBody.EndTime,
            input.RequestBody.Comment
        );
    }
}

public class CreateTimeEntryCommandToResponseMapper
    : IMappingConfig<CreateTimeEntryCommand, CreateTimeEntry.CreateTimeEntryResponse>
{
    public CreateTimeEntry.CreateTimeEntryResponse Map(CreateTimeEntryCommand input)
    {
        return new CreateTimeEntry.CreateTimeEntryResponse(
            input.Id,
            input.WorkItemId,
            input.Date,
            input.Hours,
            input.StartTime,
            input.EndTime,
            input.Comment
        );
    }
}