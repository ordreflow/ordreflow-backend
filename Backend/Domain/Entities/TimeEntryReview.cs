using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class TimeEntryReview
{
    public Guid Id { get; set; }
    public Guid TimeEntryId { get; set; }
    public Guid ReviewerId { get; set; }
    public string Description { get; set; } = null!;
    public ReviewStatus Status { get; set; }
}
