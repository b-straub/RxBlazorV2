using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Extensions;

/// <summary>
/// Extensions for analyzing partial constructor declarations to extract DI dependencies.
/// </summary>
public static class ConstructorAnalysisExtensions
{
    /// <summary>
    /// Extracts model references and DI fields from partial constructor parameters.
    /// Parameters that are ObservableModels become ModelReferenceInfo, others become DIFieldInfo.
    /// Returns unregistered services and information about DI fields for scope checking.
    /// </summary>
    public static (List<ModelReferenceInfo> modelReferences, List<DIFieldInfo> diFields, List<(string parameterName, string parameterType, ITypeSymbol? typeSymbol, Location? location)> unregisteredServices, List<(DIFieldInfo diField, string? serviceScope, Location? location)> diFieldsWithScope) ExtractPartialConstructorDependencies(
        this ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        ServiceInfoList? serviceClasses = null)
    {
        var modelReferences = new List<ModelReferenceInfo>();
        var diFields = new List<DIFieldInfo>();
        var unregisteredServices = new List<(string parameterName, string parameterType, ITypeSymbol? typeSymbol, Location? location)>();
        var diFieldsWithScope = new List<(DIFieldInfo diField, string? serviceScope, Location? location)>();

        // Find all partial constructors
        var partialConstructors = classDecl.Members
            .OfType<ConstructorDeclarationSyntax>()
            .Where(c => c.Modifiers.Any(SyntaxKind.PartialKeyword))
            .ToList();

        if (!partialConstructors.Any())
        {
            return (modelReferences, diFields, unregisteredServices, diFieldsWithScope);
        }

        // Use the first partial constructor (there should only be one)
        var constructor = partialConstructors.First();

        foreach (var parameter in constructor.ParameterList.Parameters)
        {
            if (parameter.Type is null)
            {
                continue;
            }

            var parameterType = semanticModel.GetTypeInfo(parameter.Type).Type;
            if (parameterType is null)
            {
                continue;
            }

            var parameterName = parameter.Identifier.ValueText;
            var parameterTypeName = parameterType.ToDisplayString();
            var propertyName = ToPascalCase(parameterName);

            // Check if this parameter is an ObservableModel
            if (parameterType is INamedTypeSymbol namedType && namedType.InheritsFromObservableModel())
            {
                // This is an ObservableModel - create a ModelReferenceInfo
                // Analyze usage to determine which properties are accessed
                var usedProperties = classDecl.AnalyzeModelReferenceUsage(namedType);

                // Check if this is a derived ObservableModel (inherits from another ObservableModel)
                var baseObservableModelType = namedType.GetObservableModelBaseType();
                var isDerivedModel = baseObservableModelType != null;
                var baseTypeName = baseObservableModelType?.ToDisplayString();

                modelReferences.Add(new ModelReferenceInfo(
                    parameterTypeName,
                    namedType.ContainingNamespace.ToDisplayString(),
                    propertyName,
                    usedProperties,
                    parameter.Type?.GetLocation(),
                    isDerivedModel,
                    baseTypeName));

                // Also track scope for ObservableModel dependencies (for scope violation checking)
                var diField = new DIFieldInfo(propertyName, parameterTypeName);
                var serviceScope = GetServiceScope(parameterTypeName, serviceClasses, namedType);
                diFieldsWithScope.Add((diField, serviceScope, parameter.Type?.GetLocation()));
            }
            else if (parameterType is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.InheritsFromIObservableModel())
            {
                // This is an IObservableModel interface - analyze usage to determine which properties are accessed
                var usedProperties = classDecl.AnalyzeModelReferenceUsage(namedTypeSymbol);

                modelReferences.Add(new ModelReferenceInfo(
                    parameterTypeName,
                    namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                    propertyName,
                    usedProperties,
                    parameter.Type?.GetLocation()));

                // Also track scope for IObservableModel dependencies (for scope violation checking)
                var diField = new DIFieldInfo(propertyName, parameterTypeName);
                var serviceScope = GetServiceScope(parameterTypeName, serviceClasses, namedTypeSymbol);
                diFieldsWithScope.Add((diField, serviceScope, parameter.Type?.GetLocation()));
            }
            else
            {
                // All other parameters are treated as DI services - always generate the property and constructor
                // to avoid confusing "Partial member must have an implementation part" errors
                var diField = new DIFieldInfo(propertyName, parameterTypeName);
                diFields.Add(diField);

                // Check if this service is registered in DI - if not, track it for a warning
                var isRegistered = parameterType.IsDIInjectableType(semanticModel, serviceClasses);
                if (!isRegistered)
                {
                    unregisteredServices.Add((parameterName, parameterTypeName, parameterType, parameter.Type?.GetLocation()));
                }

                // Try to detect service scope for scope violation checking
                var serviceScope = GetServiceScope(parameterTypeName, serviceClasses, parameterType);
                diFieldsWithScope.Add((diField, serviceScope, parameter.Type?.GetLocation()));
            }
        }

        return (modelReferences, diFields, unregisteredServices, diFieldsWithScope);
    }

    /// <summary>
    /// Attempts to determine the DI scope of a service type.
    /// Returns "Singleton", "Scoped", "Transient", or null if cannot be determined.
    /// For ObservableModels, checks the ObservableModelScope attribute.
    /// For other services, looks in the detected service list from ServiceAnalyzer.
    /// </summary>
    private static string? GetServiceScope(string serviceTypeName, ServiceInfoList? serviceClasses, ITypeSymbol? typeSymbol = null)
    {
        // If we have a type symbol, check if it's an ObservableModel with a scope attribute
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.InheritsFromObservableModel())
        {
            // Extract scope from ObservableModelScope attribute
            foreach (var attribute in namedTypeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "ObservableModelScopeAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0 &&
                        attribute.ConstructorArguments[0].Value is int scopeValue)
                    {
                        return scopeValue switch
                        {
                            0 => "Singleton",
                            1 => "Scoped",
                            2 => "Transient",
                            _ => "Singleton" // Default
                        };
                    }
                }
            }
            // Default scope for ObservableModels without explicit attribute
            return "Singleton";
        }

        if (serviceClasses is null)
        {
            return null;
        }

        // Look for the service in the detected service list
        var serviceInfo = serviceClasses.Services.FirstOrDefault(s =>
            s.FullyQualifiedName == serviceTypeName ||
            s.ClassName == serviceTypeName);

        return serviceInfo?.ServiceScope;
    }

    /// <summary>
    /// Checks if a class has a partial constructor declaration.
    /// </summary>
    public static bool HasPartialConstructor(this ClassDeclarationSyntax classDecl)
    {
        return classDecl.Members
            .OfType<ConstructorDeclarationSyntax>()
            .Any(c => c.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    /// <summary>
    /// Gets the partial constructor declaration if it exists.
    /// </summary>
    public static ConstructorDeclarationSyntax? GetPartialConstructor(this ClassDeclarationSyntax classDecl)
    {
        return classDecl.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault(c => c.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    /// <summary>
    /// Extracts the accessibility modifier from a partial constructor declaration.
    /// Returns "public", "protected", "private", "internal", "protected internal", or "private protected".
    /// Defaults to "public" if no explicit accessibility modifier is found.
    /// </summary>
    public static string GetConstructorAccessibility(this ClassDeclarationSyntax classDecl)
    {
        var constructor = classDecl.GetPartialConstructor();
        if (constructor is null)
        {
            return "public";
        }

        var modifiers = constructor.Modifiers;

        // Check for compound modifiers first
        if (modifiers.Any(SyntaxKind.ProtectedKeyword) && modifiers.Any(SyntaxKind.InternalKeyword))
        {
            return "protected internal";
        }
        if (modifiers.Any(SyntaxKind.PrivateKeyword) && modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return "private protected";
        }

        // Check for single modifiers
        if (modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return "public";
        }
        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
        {
            return "protected";
        }
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            return "private";
        }
        if (modifiers.Any(SyntaxKind.InternalKeyword))
        {
            return "internal";
        }

        // Default to public if no accessibility modifier found
        return "public";
    }

    /// <summary>
    /// Converts a camelCase or snake_case string to PascalCase.
    /// </summary>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Handle snake_case
        if (input.Contains('_'))
        {
            var parts = input.Split('_');
            return string.Join("", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));
        }

        // Convert first character to uppercase for camelCase
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    /// <summary>
    /// Converts a PascalCase string to camelCase by lowering the first character.
    /// </summary>
    public static string ToCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Convert first character to lowercase
        return char.ToLower(input[0]) + input.Substring(1);
    }
}
