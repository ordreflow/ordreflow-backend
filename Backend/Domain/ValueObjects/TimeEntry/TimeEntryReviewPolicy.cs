namespace Domain.ValueObjects;

public sealed record TimeEntryReviewPolicy(bool AllowSelfReview = true);
