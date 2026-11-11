using Domain.ValueObjects;

namespace Domain.Entities;

/*  Summary
 *  when a Time entry is reviewed
 */
public sealed class TimeEntryReview
{
    public Guid Id { get; set; }
    public Guid TimeEntryId { get; set; }
    
    // Who reviewed it
    public Guid ReviewerId { get; set; }
    
    // optional description
    public string? Description { get; set; }
    
    // approved/denied
    public ReviewStatus Status { get; set; }
}
