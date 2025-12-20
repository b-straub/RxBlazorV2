using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RxBlazorV2Generator.Extensions;

public static class TypeSymbolExtensions
{
    public static bool IsObservableModelClass(this ClassDeclarationSyntax classDecl)
    {
        // Check if class has a base list with types
        if (classDecl.BaseList?.Types.Any() == true)
        {
            // Two-tier filter to minimize semantic analysis overhead:
            // 1. Clear cases: Direct ObservableModel inheritance (fast path)
            var hasDirect = classDecl.BaseList.Types.Any(t =>
            {
                var typeString = t.Type.ToString();
                return typeString.Equals("ObservableModel") ||
                       typeString.EndsWith(".ObservableModel");
            });

            if (hasDirect)
            {
                return true;
            }

            // 2. Candidates: Other partial classes with base types that could be derived ObservableModels
            // Exclude obvious non-candidates to reduce semantic analysis load
            var hasCandidate = classDecl.BaseList.Types.Any(t =>
            {
                var typeString = t.Type.ToString();
                // Exclude common non-ObservableModel base types
                return !typeString.Contains("ComponentBase") &&
                       !typeString.Contains("Exception") &&
                       !typeString.Contains("Attribute");
            });

            if (hasCandidate)
            {
                return true;
            }
        }

        // 3. Partial class declarations without base types - could be part of an ObservableModel
        // These will be semantically verified later in ObservableModelRecord.Create
        if (classDecl.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
        {
            // Include if it has RxBlazor-related attributes (fast heuristic)
            var hasRxBlazorAttribute = classDecl.AttributeLists.Any(al =>
                al.Attributes.Any(a =>
                {
                    var name = a.Name.ToString();
                    return name.Contains("Observable") ||
                           name.Contains("ObservableCommand") ||
                           name.Contains("ObservableComponent") ||
                           name.Contains("ObservableModelScope");
                }));

            // Or if it has members with RxBlazor attributes
            if (!hasRxBlazorAttribute)
            {
                hasRxBlazorAttribute = classDecl.Members.Any(m =>
                    m is PropertyDeclarationSyntax prop &&
                    prop.AttributeLists.Any(al =>
                        al.Attributes.Any(a =>
                        {
                            var name = a.Name.ToString();
                            return name.Contains("ObservableCommand") ||
                                   name.Contains("ObservableTrigger") ||
                                   name.Contains("ObservableComponentTrigger");
                        })));
            }

            return hasRxBlazorAttribute;
        }

        return false;
    }

    public static bool InheritsFromObservableModel(this INamedTypeSymbol classSymbol)
    {
        // Check if class inherits from ObservableModel
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "ObservableModel")
                return true;
            baseType = baseType.BaseType;
        }

        return false;
    }

    public static INamedTypeSymbol? GetObservableModelBaseType(this INamedTypeSymbol classSymbol)
    {
        // Get the immediate base type
        var baseType = classSymbol.BaseType;

        if (baseType is null)
        {
            return null;
        }

        // If the immediate base type is ObservableModel itself, no intermediate base
        if (baseType.Name == "ObservableModel")
        {
            return null;
        }

        // If the immediate base type is StatusBaseModel (abstract base), no intermediate base
        // StatusBaseModel is intended to be inherited for custom status handling
        if (baseType.Name == "StatusBaseModel" && baseType.IsAbstract)
        {
            return null;
        }

        // Check if the base type inherits from ObservableModel
        // If so, this class is a "derived" model (not directly inheriting from ObservableModel)
        if (baseType.InheritsFromObservableModel())
        {
            return baseType;
        }

        return null;
    }
    
    public static bool InheritsFromIObservableModel(this INamedTypeSymbol typeSymbol)
    {
        // Check if type implements IObservableModel interface
        if (typeSymbol.TypeKind == TypeKind.Interface)
        {
            // For interfaces, check if it inherits from IObservableModel
            if (typeSymbol.Name == "IObservableModel")
                return true;

            foreach (var interfaceType in typeSymbol.Interfaces)
            {
                if (interfaceType.Name == "IObservableModel" || interfaceType.InheritsFromIObservableModel())
                    return true;
            }
        }
        else
        {
            // For classes, check if they implement IObservableModel
            foreach (var interfaceType in typeSymbol.AllInterfaces)
            {
                if (interfaceType.Name == "IObservableModel")
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the type inherits from StatusBaseModel class.
    /// StatusBaseModel is used for centralized error and status message handling.
    /// </summary>
    public static bool InheritsFromStatusModel(this INamedTypeSymbol typeSymbol)
    {
        // Check inheritance chain for StatusBaseModel
        var baseType = typeSymbol.BaseType;
        while (baseType is not null)
        {
            if (baseType.Name == "StatusBaseModel" &&
                baseType.ContainingNamespace?.ToDisplayString() == "RxBlazorV2.Model")
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
    
    public static List<string> ExtractObservableModelInterfaces(this INamedTypeSymbol typeSymbol)
    {
        var observableInterfaces = new List<string>();
        
        // Check all implemented interfaces
        foreach (var interfaceType in typeSymbol.AllInterfaces)
        {
            // Include interfaces that inherit from IObservableModel (but not IObservableModel itself 
            // since that's handled by the base class)
            if (interfaceType.Name != "IObservableModel" && interfaceType.InheritsFromIObservableModel())
            {
                observableInterfaces.Add(interfaceType.ToDisplayString());
            }
        }
        
        return observableInterfaces;
    }
    
    public static string ExtractObservableModelGenericTypes(this INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType)
        {
            return string.Empty;
        }
        
        var genericTypes = new List<string>();
        
        // Check all implemented interfaces
        foreach (var genericType in typeSymbol.TypeArguments)
        {
            genericTypes.Add(genericType.ToDisplayString());
        }
        
        var generics = genericTypes.Aggregate((f, n) =>
            f + ", " + n);

        char[] trims = [' ', ','];
        generics = generics.TrimEnd(trims);
            
        if (generics.Length > 0)
        {
            generics = "<" + generics + ">";
        }
        
        return generics;
    }
    
    public static bool IsServiceRegistration(this InvocationExpressionSyntax invocation)
    {
        // Check if this is a service registration call like services.AddScoped<T>(), services.AddSingleton<T>(), etc.
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.ValueText;
            return methodName.StartsWith("Add") && 
                   (methodName.Contains("Scoped") || methodName.Contains("Singleton") || methodName.Contains("Transient"));
        }
        return false;
    }
}