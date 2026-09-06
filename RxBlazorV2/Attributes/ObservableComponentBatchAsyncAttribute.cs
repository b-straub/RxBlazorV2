#nullable enable
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Groups several properties into one batch that generates a single async component hook.
/// Every property carrying the same <paramref name="batchId"/> feeds the generated
/// <c>On{BatchId}BatchChangedAsync(CancellationToken)</c> method on the model's component.
/// </summary>
/// <param name="batchId">
/// Identifier of the batch. Properties sharing it feed the same hook, and the hook method name is
/// derived from it, so it must be a valid C# identifier.
/// </param>
/// <param name="debounceMilliseconds">
/// Trailing debounce window for <b>this property</b>. Zero (the default) means the property reaches
/// the hook immediately - the right choice for a discrete action such as a checkbox or switch.
/// A positive value lets a burst of changes settle first, which is what typing into a text field
/// needs.
/// </param>
/// <remarks>
/// <para>
/// The window is per property, not per batch, so one batch can mix both kinds. The generator emits
/// one filtered stream per distinct window and merges them into a single subscription.
/// </para>
/// <para>
/// The whole batch shares one <c>AwaitOperation.Switch</c> dispatch: a newer change cancels the
/// token of an in-flight hook invocation instead of queueing behind it.
/// </para>
/// <para>
/// Unrelated to <see cref="ObservableBatchAttribute"/>, which groups properties for
/// <c>SuspendNotifications</c> scopes and generates nothing.
/// </para>
/// <para>
/// Requires <c>[ObservableComponent]</c> on the model - without it no hook is generated
/// (see <see href="https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG044.md">RXBG044</see>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typing settles before it reaches the backend.
/// [ObservableComponentBatchAsync("search", 250)]
/// public partial string SearchTerm { get; set; } = "";
///
/// // A switch click is deliberate - react at once.
/// [ObservableComponentBatchAsync("search")]
/// public partial bool HighlightMatches { get; set; }
///
/// // Generated on the component:
/// // protected virtual Task OnSearchBatchChangedAsync(CancellationToken ct)
/// </code>
/// </example>
#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableComponentBatchAsyncAttribute(string batchId, int debounceMilliseconds = 0) : Attribute
{
    /// <summary>
    /// Gets the batch identifier. Properties sharing it feed the same generated hook.
    /// </summary>
    public string BatchId { get; } = batchId;

    /// <summary>
    /// Gets this property's trailing debounce window in milliseconds, or zero for immediate delivery.
    /// </summary>
    public int DebounceMilliseconds { get; } = debounceMilliseconds;
}
#pragma warning restore CS9113
