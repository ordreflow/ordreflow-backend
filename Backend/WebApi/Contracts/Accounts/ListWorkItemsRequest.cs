namespace WebAPI.Contracts.Accounts;

public sealed record ListWorkItemsRequest(bool AssignedToMe = false, string? Search = null);
