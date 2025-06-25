#nullable enable
namespace RxBlazorV2.Model;

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ObservableCommandTriggerAttribute<T>(string triggerProperty, T parameter, string? canTriggerMethodName = null)
    : Attribute
{
}
#pragma warning restore CS9113