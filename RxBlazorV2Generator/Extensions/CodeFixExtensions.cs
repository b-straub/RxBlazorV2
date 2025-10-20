using Microsoft.CodeAnalysis;

namespace RxBlazorV2Generator.Extensions;

public static class CodeFixExtensions
{
    /// <summary>
    /// Gets the code fix message from the diagnostic descriptor's custom tags.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor</param>
    /// <param name="index">The index of the code fix message in custom tags (default: 0)</param>
    /// <param name="parameters">Optional format parameters for the message</param>
    /// <returns>The formatted code fix message</returns>
    public static string CodeFixMessage(this DiagnosticDescriptor descriptor, int index = 0, params string[] parameters)
    {
        var message = descriptor.CustomTags.Skip(index).FirstOrDefault() ?? string.Empty;
        if (parameters.Any())
        {
            message = string.Format(message, parameters);
        }
        return message;
    }

    /// <summary>
    /// Gets all code fix messages from the diagnostic descriptor's custom tags.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor</param>
    /// <param name="parameters">Optional format parameters for the messages</param>
    /// <returns>Enumerable of all code fix messages</returns>
    public static IEnumerable<string> CodeFixMessages(this DiagnosticDescriptor descriptor, params string[] parameters)
    {
        var messages = descriptor.CustomTags;
        if (parameters.Any())
        {
            messages = messages.Select(m => string.Format(m, parameters));
        }
        return messages;
    }

    /// <summary>
    /// Gets a property value from the diagnostic's properties collection.
    /// </summary>
    /// <param name="diagnostic">The diagnostic</param>
    /// <param name="property">The property key</param>
    /// <returns>The property value or null if not found</returns>
    public static string? CodeFixProperty(this Diagnostic diagnostic, string property)
    {
        if (diagnostic.Properties.TryGetValue(property, out var value))
        {
            return value;
        }
        return null;
    }
}
