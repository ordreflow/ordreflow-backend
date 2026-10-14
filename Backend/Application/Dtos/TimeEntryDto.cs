namespace Application.Dtos;

public sealed record TimeEntryDto(
    Guid Id,
    Guid TaskId,
    DateTime Date,
    decimal Hours,
    string? Comment);
