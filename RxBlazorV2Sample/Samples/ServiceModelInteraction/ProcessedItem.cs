namespace RxBlazorV2Sample.Samples.ServiceModelInteraction;

/// <summary>
/// Domain model representing a processed item.
/// NOT an EventArgs - just a data record.
/// </summary>
public record ProcessedItem(
    Guid Id,
    string Input,
    string Result,
    DateTime ProcessedAt);
