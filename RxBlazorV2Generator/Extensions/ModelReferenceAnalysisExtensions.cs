using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Extensions;

public static class ModelReferenceAnalysisExtensions
{
    public static List<string> AnalyzeModelReferenceUsage(this ClassDeclarationSyntax classDecl, string referencedModelName)
    {
        var usedProperties = new HashSet<string>();
        
        // Look for usage patterns like "referencedModelName.PropertyName" or "ReferencedModelName.PropertyName"
        foreach (var node in classDecl.DescendantNodes())
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var expression = memberAccess.Expression.ToString();
                
                // Check for direct reference (e.g., "CounterModel.Counter1")
                if (expression.Equals(referencedModelName, StringComparison.OrdinalIgnoreCase))
                {
                    usedProperties.Add(memberAccess.Name.Identifier.ValueText);
                }
                // Check for property access through the generated property name
                else if (expression.EndsWith($".{referencedModelName}", StringComparison.OrdinalIgnoreCase))
                {
                    usedProperties.Add(memberAccess.Name.Identifier.ValueText);
                }
            }
        }
        
        return usedProperties.ToList();
    }

    public static List<string> AnalyzeCommandMethodsForModelReferences(this ObservableModelInfo modelInfo, 
        CommandPropertyInfo command, 
        string referencedModelName)
    {
        var usedProperties = new HashSet<string>();
        
        // Analyze execute method for model property usage
        if (command.ExecuteMethod != null && modelInfo.Methods.TryGetValue(command.ExecuteMethod, out var executeMethod))
        {
            var executeProps = executeMethod.AnalyzeMethodForModelReferences(referencedModelName);
            foreach (var prop in executeProps)
                usedProperties.Add(prop);
        }
        
        // Analyze canExecute method for model property usage
        if (command.CanExecuteMethod != null && modelInfo.Methods.TryGetValue(command.CanExecuteMethod, out var canExecuteMethod))
        {
            var canExecuteProps = canExecuteMethod.AnalyzeMethodForModelReferences(referencedModelName);
            foreach (var prop in canExecuteProps)
                usedProperties.Add(prop);
        }
        
        return usedProperties.ToList();
    }

    public static List<ModelReferenceInfo> EnhanceModelReferencesWithCommandAnalysis(this ObservableModelInfo modelInfo)
    {
        var enhancedModelReferences = new List<ModelReferenceInfo>();
        
        foreach (var modelRef in modelInfo.ModelReferences)
        {
            var allUsedProperties = new HashSet<string>(modelRef.UsedProperties);
            
            // Analyze command methods for additional property references
            foreach (var cmd in modelInfo.CommandProperties)
            {
                var cmdUsedProps = modelInfo.AnalyzeCommandMethodsForModelReferences(cmd, modelRef.ReferencedModelTypeName);
                foreach (var prop in cmdUsedProps)
                {
                    allUsedProperties.Add(prop);
                }
            }
            
            enhancedModelReferences.Add(new ModelReferenceInfo(
                modelRef.ReferencedModelTypeName,
                modelRef.ReferencedModelNamespace,
                modelRef.PropertyName,
                allUsedProperties.ToList()));
        }

        return enhancedModelReferences;
    }
}