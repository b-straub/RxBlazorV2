using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace RxBlazorV2Generator.Diagnostics;

/// <summary>
/// Suppresses RXBG020 (unregistered service warning) for services from external libraries
/// when those libraries provide service registration extension methods that are called.
///
/// Example: ISnackbar from MudBlazor → checks if AddMudServices() is called
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ExternalServiceSuppressor : DiagnosticSuppressor
{
    private static readonly string SuppressedDiagnosticId = DiagnosticDescriptors.UnregisteredServiceWarning.Id;

    private static readonly SuppressionDescriptor ExternalServiceSuppression = new(
        id: "RXBGSP001",
        suppressedDiagnosticId: SuppressedDiagnosticId,
        justification: "Service type is from an external library that provides DI extension methods (e.g., AddMudServices), and those methods are called in the code.");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        ImmutableArray.Create(ExternalServiceSuppression);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Id != SuppressedDiagnosticId)
            {
                continue;
            }

            // Get the location and semantic model
            var syntaxTree = diagnostic.Location.SourceTree;
            if (syntaxTree is null)
            {
                continue;
            }

            var compilation = context.GetSemanticModel(syntaxTree).Compilation;

            // Extract type information from diagnostic properties
            if (!diagnostic.Properties.TryGetValue("TypeName", out var typeName) ||
                string.IsNullOrEmpty(typeName))
            {
                continue;
            }

            // Explicit null check for flow analysis (should never be null after above check)
            if (typeName is null)
            {
                throw new InvalidOperationException($"TypeName property was unexpectedly null for diagnostic {diagnostic.Id}");
            }

            var typeSymbol = compilation.GetTypeByMetadataName(typeName);
            if (typeSymbol is null)
            {
                continue;
            }

            // Check if type is from an external assembly AND has likely service registration
            if (IsExternalLibraryTypeWithRegistration(typeSymbol, compilation))
            {
                context.ReportSuppression(Suppression.Create(ExternalServiceSuppression, diagnostic));
            }
        }
    }

    /// <summary>
    /// Determines if a type is from an external library AND that library's service registration
    /// extension methods are likely called in the code.
    /// </summary>
    private static bool IsExternalLibraryTypeWithRegistration(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var containingAssembly = typeSymbol.ContainingAssembly;
        if (containingAssembly is null)
        {
            return false;
        }

        var assemblyName = containingAssembly.Name;
        var compilationAssemblyName = compilation.AssemblyName;

        // Not external if it's from the same assembly
        if (assemblyName == compilationAssemblyName)
        {
            return false;
        }

        // Check if it's a well-known library with known extension methods
        if (IsWellKnownExternalLibrary(assemblyName))
        {
            // Check if the corresponding extension method is called
            var extensionMethodName = GetServiceRegistrationMethodName(assemblyName);
            if (extensionMethodName is not null && IsExtensionMethodCalled(compilation, extensionMethodName))
            {
                return true;
            }
        }

        // For other external assemblies, check if there are service registration patterns called
        return HasServiceRegistrationPattern(compilation, assemblyName);
    }

    /// <summary>
    /// Checks if a specific extension method is called anywhere in the compilation.
    /// </summary>
    private static bool IsExtensionMethodCalled(Compilation compilation, string methodName)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Name.Identifier.ValueText == methodName)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if there's a pattern suggesting service registration for a specific assembly
    /// (e.g., "Add{AssemblyName}" or "Use{AssemblyName}").
    /// </summary>
    private static bool HasServiceRegistrationPattern(Compilation compilation, string assemblyName)
    {
        // Common patterns: AddMudBlazor, AddMudServices, UseMudBlazor, etc.
        // Extract base name (e.g., "MudBlazor" from "MudBlazor.Services")
        var baseName = assemblyName.Split('.')[0];

        var patterns = new[]
        {
            $"Add{baseName}",
            $"Add{baseName}Services",
            $"Use{baseName}",
            $"Configure{baseName}"
        };

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var methodName = memberAccess.Name.Identifier.ValueText;
                    foreach (var pattern in patterns)
                    {
                        if (methodName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Maps well-known assembly names to their service registration extension method names.
    /// </summary>
    private static string? GetServiceRegistrationMethodName(string assemblyName)
    {
        // Map assembly names to their typical service registration method names
        if (assemblyName.StartsWith("MudBlazor"))
        {
            return "AddMudServices";
        }

        if (assemblyName == "System.Net.Http")
        {
            return "AddHttpClient";
        }

        if (assemblyName.StartsWith("Microsoft.AspNetCore."))
        {
            // Various ASP.NET Core services have different registration methods
            // Return null to use pattern matching instead
            return null;
        }

        if (assemblyName.StartsWith("Microsoft.Extensions."))
        {
            // Extensions libraries typically register automatically or via specific methods
            return null;
        }

        return null;
    }

    /// <summary>
    /// Determines if a type is from an external library (not the user's project).
    /// </summary>
    private static bool IsExternalLibraryType(ITypeSymbol typeSymbol, Compilation compilation)
    {
        var containingAssembly = typeSymbol.ContainingAssembly;
        if (containingAssembly is null)
        {
            return false;
        }

        // Get the assembly name
        var assemblyName = containingAssembly.Name;

        // Check if it's from a well-known external library
        if (IsWellKnownExternalLibrary(assemblyName))
        {
            return true;
        }

        // Check if the assembly is different from the compilation assembly
        // and not from the RxBlazorV2 project itself
        var compilationAssemblyName = compilation.AssemblyName;
        if (assemblyName != compilationAssemblyName &&
            !assemblyName.StartsWith("RxBlazorV2"))
        {
            // It's from a different assembly - likely external
            // But exclude common test/generated assemblies
            if (assemblyName.Contains("Test") ||
                assemblyName.Contains("Generated") ||
                assemblyName == "mscorlib" ||
                assemblyName == "System.Private.CoreLib")
            {
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the assembly name matches well-known external libraries
    /// that provide DI services.
    /// </summary>
    private static bool IsWellKnownExternalLibrary(string assemblyName)
    {
        // Well-known libraries that provide DI services
        return assemblyName.StartsWith("MudBlazor") ||
               assemblyName.StartsWith("Microsoft.Extensions.") ||
               assemblyName.StartsWith("Microsoft.AspNetCore.") ||
               assemblyName == "System.Net.Http" ||
               assemblyName.StartsWith("Blazored.") ||
               assemblyName.StartsWith("MatBlazor") ||
               assemblyName.StartsWith("Radzen.") ||
               assemblyName.StartsWith("AntDesign") ||
               assemblyName.StartsWith("Syncfusion.") ||
               assemblyName.StartsWith("Telerik.");
    }
}
