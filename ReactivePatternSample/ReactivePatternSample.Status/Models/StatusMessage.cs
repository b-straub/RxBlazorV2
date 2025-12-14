namespace ReactivePatternSample.Status.Models;

/// <summary>
/// Represents a status message with severity level.
/// </summary>
public class StatusMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Message { get; init; }
    public required StatusSeverity Severity { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? Source { get; init; }
}

/// <summary>
/// Status message severity levels.
/// </summary>
public enum StatusSeverity
{
    Info,
    Success,
    Warning,
    Error
}
