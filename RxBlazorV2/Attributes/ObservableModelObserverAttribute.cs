using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Marks a method in a service class as an observer for property changes on an ObservableModel.
/// When the service is injected into the model via a partial constructor, the generator
/// automatically subscribes this method to changes of the specified property.
/// </summary>
/// <remarks>
/// <para>
/// The method must follow one of these signature patterns:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>Sync: <c>void MethodName(ModelType model)</c></description>
///   </item>
///   <item>
///     <description>Async without cancellation: <c>Task MethodName(ModelType model)</c></description>
///   </item>
///   <item>
///     <description>Async with cancellation: <c>Task MethodName(ModelType model, CancellationToken ct)</c></description>
///   </item>
/// </list>
/// <para>
/// Multiple observers can be attached to the same property by applying the attribute multiple times
/// to different methods. Each observer gets its own subscription.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyService
/// {
///     [ObservableModelObserver(nameof(MyModel.UserName))]
///     public void OnUserNameChanged(MyModel model)
///     {
///         Console.WriteLine($"User changed to: {model.UserName}");
///     }
///
///     [ObservableModelObserver(nameof(MyModel.Settings))]
///     public async Task OnSettingsChangedAsync(MyModel model, CancellationToken ct)
///     {
///         await SaveSettingsAsync(model.Settings, ct);
///     }
/// }
///
/// // In the model:
/// [ObservableModelScope(ModelScope.Scoped)]
/// public partial class MyModel : ObservableModel
/// {
///     public partial string UserName { get; set; }
///     public partial Settings Settings { get; set; }
///
///     // Service is injected and observers are auto-subscribed
///     public partial MyModel(MyService service);
/// }
/// </code>
/// </example>
/// <param name="triggerPropertyName">
/// The name of the property to observe. Use <c>nameof(ModelType.PropertyName)</c> for type safety.
/// </param>
#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ObservableModelObserverAttribute(string triggerPropertyName)
    : Attribute
{
}
#pragma warning restore CS9113