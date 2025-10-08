#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Automatically triggers a command when a specified property changes.
/// Multiple triggers can be applied to the same command property.
/// </summary>
/// <param name="triggerProperty">
/// The property that triggers command execution when it changes.
/// Supports dot notation for referenced models (e.g., "SettingsModel.AutoRefresh").
/// </param>
/// <param name="canTriggerMethodName">
/// Optional. The name of a private method that determines if the trigger should execute.
/// Must return <see langword="bool"/>. Combined with command's canExecute using logical AND.
/// </param>
/// <remarks>
/// <para><b>Warning:</b> Avoid circular triggers where the command modifies the same property it listens to.</para>
/// <para>See <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG012.md">RXBG012</see> for circular trigger troubleshooting.</para>
/// </remarks>

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableCommandTriggerAttribute(string triggerProperty, string? canTriggerMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113