using Application.Commands;
using Backend.TimeEntries;
using Domain.Entities;
using ObjectMapper;

namespace WebAPI.MappingConfigurations;

public class GetTimeEntriesToResponseMapper
	: IMappingConfig<List<TimeEntry>, GetTimeEntriesResponse>
{
	public GetTimeEntriesResponse Map(List<TimeEntry> input)
	{
		return new GetTimeEntriesResponse(
			input.Select(timeEntry => new TimeEntryDto(
				timeEntry.Id,
				timeEntry.WorkItemId,
				timeEntry.Date,
				timeEntry.Hours,
				timeEntry.StartTime,
				timeEntry.EndTime,
				timeEntry.Comment)));
	}
}
