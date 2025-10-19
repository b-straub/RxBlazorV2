using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace RxBlazorV2Generator.Exceptions;

/// <summary>
/// Exception that carries diagnostic information for source generator errors.
/// This allows validation code to throw exceptions that are automatically converted to proper Roslyn diagnostics.
/// </summary>
[ExcludeFromCodeCoverage]
public class DiagnosticException : Exception
{
    /// <summary>
    /// Gets the diagnostic descriptor that defines this error.
    /// </summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the location in source code where this diagnostic should be reported.
    /// </summary>
    public Location Location { get; }

    /// <summary>
    /// Gets the message format arguments for the diagnostic.
    /// </summary>
    public object[] MessageArgs { get; }

    /// <summary>
    /// Initializes a new instance of the DiagnosticException class.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <param name="location">The location in source code.</param>
    /// <param name="messageArgs">Optional message format arguments.</param>
    public DiagnosticException(
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs)
        : base(FormatMessage(descriptor, messageArgs))
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Location = location ?? throw new ArgumentNullException(nameof(location));
        MessageArgs = messageArgs ?? Array.Empty<object>();
    }

    /// <summary>
    /// Creates a Roslyn Diagnostic from this exception.
    /// </summary>
    /// <returns>A Diagnostic that can be reported to the compilation.</returns>
    public Diagnostic ToDiagnostic()
    {
        return Diagnostic.Create(Descriptor, Location, MessageArgs);
    }

    private static string FormatMessage(DiagnosticDescriptor descriptor, object[] messageArgs)
    {
        if (descriptor is null)
        {
            return "Diagnostic error occurred";
        }

        if (messageArgs is null || messageArgs.Length == 0)
        {
            return descriptor.MessageFormat.ToString();
        }

        return string.Format(descriptor.MessageFormat.ToString(), messageArgs);
    }
}
