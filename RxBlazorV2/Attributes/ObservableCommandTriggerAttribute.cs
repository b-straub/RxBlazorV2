#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Automatically triggers a command when a specified property changes.
/// Used in conjunction with <see cref="ObservableCommandAttribute"/> to create reactive command execution.
/// </summary>
/// <param name="triggerProperty">
/// The property that will trigger the command execution when it changes.
/// Can reference properties from the current model or referenced models using dot notation.
/// Examples: "IsEnabled", "SettingsModel.AutoRefresh", "ISettingsModel.IsDarkMode"
/// </param>
/// <param name="canTriggerMethodName">
/// Optional. The name of a private method that determines if the trigger should execute the command.
/// Must return <see langword="bool"/>. Use <see langword="nameof"/> for compile-time safety (e.g., <c>nameof(CanTrigger)</c>).
/// When omitted, the trigger automatically inherits the command's <paramref name="canExecuteMethodName"/> for validation.
/// When both <paramref name="canTriggerMethodName"/> and the command's <paramref name="canExecuteMethodName"/> are present, they are combined with logical AND (both must return <see langword="true"/>).
/// </param>
/// <remarks>
/// <para>This attribute works alongside <see cref="ObservableCommandAttribute"/> to provide automatic command execution:</para>
/// <list type="bullet">
/// <item>
/// <description>When the specified property changes, the command is automatically executed</description>
/// </item>
/// <item>
/// <description>Trigger validation automatically uses the command's <paramref name="canExecuteMethodName"/> when no <paramref name="canTriggerMethodName"/> is specified</description>
/// </item>
/// <item>
/// <description>When both <paramref name="canExecuteMethodName"/> (from command) and <paramref name="canTriggerMethodName"/> (from trigger) exist, both must return <see langword="true"/> (logical AND)</description>
/// </item>
/// <item>
/// <description>This ensures triggers respect the same execution constraints as manual command invocation</description>
/// </item>
/// <item>
/// <description>Supports properties from the current model and referenced models</description>
/// </item>
/// <item>
/// <description>Multiple triggers can be applied to the same command property</description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [ObservableModelReference&lt;ISettingsModel&gt;]
/// public partial class WeatherModel : ObservableModel
/// {
///     public partial bool IsLoading { get; set; }
///     
///     // Example 1: Trigger inherits command's canExecuteMethodName (CanRefresh)
///     [ObservableCommand(nameof(RefreshAsync), nameof(CanRefresh))]
///     [ObservableCommandTrigger(nameof(ISettingsModel.AutoRefresh))]
///     public partial IObservableCommandAsync RefreshCommand { get; }
///     
///     // Example 2: Combined validation - both CanSave AND ShouldAutoSave must return true
///     [ObservableCommand(nameof(SaveAsync), nameof(CanSave))]
///     [ObservableCommandTrigger(nameof(ISettingsModel.AutoSave), nameof(ShouldAutoSave))]
///     public partial IObservableCommandAsync SaveCommand { get; }
///     
///     private async Task RefreshAsync() { /* implementation */ }
///     private bool CanRefresh() =&gt; !IsLoading; // Automatically used for trigger validation
///     
///     private async Task SaveAsync() { /* implementation */ }
///     private bool CanSave() =&gt; !IsLoading; // Must be true (from command)
///     private bool ShouldAutoSave() =&gt; ISettingsModel.AutoSave; // Must also be true (from trigger)
/// }
/// </code>
/// </example>

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableCommandTriggerAttribute(string triggerProperty, string? canTriggerMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113