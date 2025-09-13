using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RxBlazorV2Generator.Extensions;

public static class TypeSymbolExtensions
{
    public static bool IsObservableModelClass(this ClassDeclarationSyntax classDecl)
    {
        // Only check partial classes with base types
        if (!classDecl.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
            return false;
            
        // Check if it has a base type that could be ObservableModel
        // We need to be more specific to avoid ComponentBase classes
        return classDecl.BaseList?.Types.Any(t => 
        {
            var typeString = t.Type.ToString();
            return typeString.Equals("ObservableModel") || 
                   typeString.EndsWith(".ObservableModel") ||
                   (typeString.Contains("ObservableModel") && !typeString.Contains("ComponentBase"));
        }) == true;
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