namespace ReactivePatternSample.Storage.Models;

/// <summary>
/// Represents a user in the storage.
/// </summary>
public class User
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
