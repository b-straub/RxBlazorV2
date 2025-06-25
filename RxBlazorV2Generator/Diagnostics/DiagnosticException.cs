using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace RxBlazorV2Generator.Diagnostics;

/// <summary>
/// Wrapper class for diagnostic reporting with enhanced information
/// </summary>
public class GeneratorDiagnostic
{
    public DiagnosticDescriptor Descriptor { get; }
    public Location Location { get; }
    public object[] MessageArgs { get; }
    public Dictionary<string, string?> Properties { get; }

    public GeneratorDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArgs = messageArgs;
        Properties = new Dictionary<string, string?>();
    }

    public GeneratorDiagnostic(DiagnosticDescriptor descriptor, Location location, Dictionary<string, string?> properties, params object[] messageArgs)
    {
        Descriptor = descriptor;
        Location = location;
        MessageArgs = messageArgs;
        Properties = properties ?? new Dictionary<string, string?>();
    }

    /// <summary>
    /// Creates and reports the diagnostic to the analysis context
    /// </summary>
    public void ReportDiagnostic(SyntaxNodeAnalysisContext context)
    {
        var diagnostic = Diagnostic.Create(
            Descriptor,
            Location,
            Properties.ToImmutableDictionary(),
            MessageArgs);
        
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Creates a Diagnostic instance without reporting it
    /// </summary>
    public Diagnostic CreateDiagnostic()
    {
        return Diagnostic.Create(
            Descriptor,
            Location,
            Properties.ToImmutableDictionary(),
            MessageArgs);
    }
}

/// <summary>
/// Exception wrapper for diagnostic errors
/// </summary>
public class DiagnosticException : Exception
{
    public GeneratorDiagnostic Diagnostic { get; }

    public DiagnosticException(GeneratorDiagnostic diagnostic) : base(diagnostic.Descriptor.Title.ToString())
    {
        Diagnostic = diagnostic;
    }

    public DiagnosticException(DiagnosticDescriptor descriptor, Location location, params object[] messageArgs) 
        : this(new GeneratorDiagnostic(descriptor, location, messageArgs))
    {
    }

    public DiagnosticException(DiagnosticDescriptor descriptor, Location location, Exception innerException, params object[] messageArgs) 
        : base(descriptor.Title.ToString(), innerException)
    {
        Diagnostic = new GeneratorDiagnostic(descriptor, location, messageArgs);
    }
}

/// <summary>
/// Extension methods for diagnostic reporting
/// </summary>
public static class DiagnosticExtensions
{
    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params object[] messageArgs)
    {
        var diagnostic = new GeneratorDiagnostic(descriptor, location, messageArgs);
        diagnostic.ReportDiagnostic(context);
    }

    public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, Location location, Dictionary<string, string?> properties, params object[] messageArgs)
    {
        var diagnostic = new GeneratorDiagnostic(descriptor, location, properties, messageArgs);
        diagnostic.ReportDiagnostic(context);
    }
}