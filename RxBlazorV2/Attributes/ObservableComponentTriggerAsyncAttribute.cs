using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Generates an async hook method that is called when the property value changes.
/// The hook method receives a CancellationToken parameter.
/// Use on partial properties within ObservableModel classes that have [ObservableComponent] attribute.
/// </summary>
/// <param name="hookMethodName">Optional custom name for the async hook method.
/// If not specified, defaults to On{PropertyName}ChangedAsync.</param>
/// <example>
/// [ObservableComponentTriggerAsync]
/// public partial bool IsLoading { get; set; }
/// // Generates: protected virtual Task OnIsLoadingChangedAsync(CancellationToken ct)
///
/// [ObservableComponentTriggerAsync("HandleStateChange")]
/// public partial bool IsActive { get; set; }
/// // Generates: protected virtual Task HandleStateChange(CancellationToken ct)
/// </example>
#pragma warning disable CS9113
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableComponentTriggerAsyncAttribute(string? hookMethodName = null) : Attribute
{
}
#pragma warning restore CS9113