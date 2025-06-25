using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Extensions;

namespace RxBlazorV2Generator.Extensions;

public static class DependencyInjectionAnalysisExtensions
{
    public static List<DIFieldInfo> ExtractDIFields(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel, ServiceInfoList? serviceClasses = null, ObservableModelInfo[]? observableModelClasses = null)
    {
        var diFields = new List<DIFieldInfo>();

        foreach (var member in classDecl.Members.OfType<FieldDeclarationSyntax>())
        {
            if (member.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                var fieldTypeSymbol = semanticModel.GetTypeInfo(member.Declaration.Type).Type;
                if (fieldTypeSymbol != null)
                {
                    var fieldType = fieldTypeSymbol.ToDisplayString();
                    foreach (var variable in member.Declaration.Variables)
                    {
                        var fieldName = variable.Identifier.ValueText;
                        if (fieldTypeSymbol.IsDIInjectableType(semanticModel, serviceClasses, observableModelClasses))
                        {
                            diFields.Add(new DIFieldInfo(fieldName, fieldType));
                        }
                    }
                }
            }
        }

        return diFields;
    }

    public static bool IsDIInjectableType(this ITypeSymbol typeSymbol, SemanticModel semanticModel, ServiceInfoList? serviceClasses = null, ObservableModelInfo[]? observableModelClasses = null)
    {
        var typeName = typeSymbol.ToDisplayString();
        var className = typeSymbol.Name;
        
        // First check if it's a registered service class
        if (serviceClasses?.Services.Any(s => 
            s.FullyQualifiedName == typeName || 
            s.ClassName == className ||
            s.FullyQualifiedName.EndsWith($".{className}")) == true)
        {
            return true;
        }
        
        // Check if it's an observable model class
        if (observableModelClasses?.Any(m => 
            m.FullyQualifiedName == typeName || 
            m.ClassName == className ||
            m.FullyQualifiedName.EndsWith($".{className}")) == true)
        {
            return true;
        }
        
        // Keep HttpClient as hardcoded for now since factory detection is complex
        if (typeName == "System.Net.Http.HttpClient" || className == "HttpClient")
        {
            return true;
        }
        
        return false;
    }
}