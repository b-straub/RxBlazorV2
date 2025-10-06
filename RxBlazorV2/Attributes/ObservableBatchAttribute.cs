#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Specifies a batch ID for grouping related property change notifications in an ObservableModel.
/// Multiple properties with the same batch ID can be updated together using SuspendNotifications(batchId).
/// </summary>
/// <param name="batchId">
/// A unique identifier for this batch group. Properties with the same batch ID will be notified together.
/// </param>
/// <remarks>
/// <para>Use this attribute to optimize reactive updates by batching related property changes.</para>
/// <para>When using SuspendNotifications(batchId), only notifications for properties with matching batch IDs are suspended.</para>
/// <para>If SuspendNotifications(null) is called, all notifications are suspended regardless of batch ID.</para>
/// </remarks>
/// <example>
/// <code>
/// public partial class MyModel : ObservableModel
/// {
///     [ObservableBatch("UserInfo")]
///     public partial string FirstName { get; set; }
///
///     [ObservableBatch("UserInfo")]
///     public partial string LastName { get; set; }
///
///     [ObservableBatch("Address")]
///     public partial string City { get; set; }
///
///     public void UpdateUserInfo()
///     {
///         // Only suspends UserInfo batch notifications
///         using (SuspendNotifications("UserInfo"))
///         {
///             FirstName = "John";
///             LastName = "Doe";
///             // Single notification fired at end of block
///         }
///     }
/// }
/// </code>
/// </example>
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
