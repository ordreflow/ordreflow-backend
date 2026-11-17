namespace WebAPI.Contracts.Users;

/// <summary>An empty or missing job title clears the user's title.</summary>
public sealed record ChangeJobTitleRequest(string? JobTitle);
