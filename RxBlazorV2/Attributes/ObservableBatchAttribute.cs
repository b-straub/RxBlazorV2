#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Groups related properties for batched change notifications.
/// Use with SuspendNotifications(batchId) to update multiple properties with a single notification.
/// </summary>
/// <param name="batchId">
/// A unique identifier for this batch group.
/// </param>
#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableBatchAttribute(string batchId) : Attribute
{
    /// <summary>
    /// Gets the batch identifier for grouping property notifications.
    /// </summary>
    public string BatchId { get; } = batchId;
}
#pragma warning restore CS9113
