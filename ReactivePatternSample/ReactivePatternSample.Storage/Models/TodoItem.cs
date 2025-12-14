namespace ReactivePatternSample.Storage.Models;

/// <summary>
/// Represents a todo item in the storage.
/// </summary>
public class TodoItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public required string OwnerId { get; init; }
}
