using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Diagnostics;

namespace RxBlazorV2Generator.Extensions;

public static class AttributeAnalysisExtensions
{
    public static (INamedTypeSymbol? typeSymbol, Diagnostic? diagnostic) ExtractReferencedModelTypeWithDiagnostic(this AttributeSyntax attribute, SemanticModel semanticModel, ServiceInfoList? serviceInfoList = null)
    {
        try
        {
            // Try to get the type argument from the generic attribute syntax
            if (attribute.Name is GenericNameSyntax genericName && genericName.TypeArgumentList?.Arguments.Count > 0)
            {
                var typeArgument = genericName.TypeArgumentList.Arguments.First();
                var typeInfo = semanticModel.GetTypeInfo(typeArgument);
                
                if (typeInfo.Type is INamedTypeSymbol namedTypeSymbol)
                {
                    // Check if type inherits from ObservableModel or IObservableModel
                    if (!namedTypeSymbol.InheritsFromObservableModel() && !namedTypeSymbol.InheritsFromIObservableModel())
                    {
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.InvalidModelReferenceTargetError,
                            attribute.GetLocation(),
                            namedTypeSymbol.Name);
                        return (null, diagnostic);
                    }

                    // Check if type is registered in service collection
                    if (serviceInfoList != null)
                    {
                        var fullyQualifiedName = namedTypeSymbol.ToDisplayString();
                        var className = namedTypeSymbol.Name;
                        
                        bool isRegistered = serviceInfoList.Services.Any(service => 
                            service.FullyQualifiedName == fullyQualifiedName || 
                            service.ClassName == className ||
                            service.FullyQualifiedName.EndsWith($".{className}"));

                        // Skip validation for ObservableModel types and interfaces since they're auto-registered
                        bool isObservableModel = namedTypeSymbol.InheritsFromObservableModel();
                        bool isObservableInterface = namedTypeSymbol.InheritsFromIObservableModel();
                        
                        if (!isRegistered && !isObservableModel && !isObservableInterface)
                        {
                            var diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.ModelReferenceNotRegisteredError,
                                attribute.GetLocation(),
                                className);
                            return (namedTypeSymbol, diagnostic); // Return the type but with diagnostic
                        }
                    }

                    return (namedTypeSymbol, null);
                }
            }
        }
        catch (Exception)
        {
            // Return null on any error during extraction
        }
        return (null, null);
    }

    public static INamedTypeSymbol? ExtractReferencedModelType(this AttributeSyntax attribute, SemanticModel semanticModel)
    {
        var (typeSymbol, _) = attribute.ExtractReferencedModelTypeWithDiagnostic(semanticModel);
        return typeSymbol;
    }

    public static string ExtractModelScope(this AttributeSyntax attribute, SemanticModel semanticModel)
    {
        try
        {
            // Look for the argument to the ObservableModelScopeAttribute
            var firstArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
            if (firstArgument != null)
            {
                var expression = firstArgument.Expression.ToString();
                
                // Handle enum member access like ModelScope.Scoped
                if (expression.Contains('.'))
                {
                    var parts = expression.Split('.');
                    if (parts.Length == 2 && parts[0] == "ModelScope")
                    {
                        return parts[1]; // Return "Scoped", "Singleton", etc.
                    }
                }
                // Handle direct enum value
                else if (expression.StartsWith("ModelScope"))
                {
                    return expression.Replace("ModelScope.", "");
                }
            }
        }
        catch (Exception)
        {
            // Return empty string on any error during extraction
        }
        return "";
    }

    public static (List<CommandPropertyInfo> commandProperties, List<Diagnostic> diagnostics) ExtractCommandPropertiesWithDiagnostics(this ClassDeclarationSyntax classDecl, Dictionary<string, MethodDeclarationSyntax> methods, SemanticModel? semanticModel = null)
    {
        var commandProperties = new List<CommandPropertyInfo>();
        var diagnostics = new List<Diagnostic>();
        
        foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            var attributes = member.AttributeLists.SelectMany(al => al.Attributes).ToArray();
            var commandAttr = attributes.FirstOrDefault(a => 
                a.Name.ToString().Contains("ObservableCommandAsync") ||
                a.Name.ToString().Contains("ObservableCommand"));
            
            if (commandAttr != null)
            {
                var executeMethodArg = commandAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString();
                var canExecuteMethodArg = commandAttr.ArgumentList?.Arguments.Skip(1).FirstOrDefault()?.Expression.ToString();
                
                // Remove nameof() wrapper and quotes
                var executeMethod = executeMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');
                var canExecuteMethod = canExecuteMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');

                // Analyze the execute method to determine if it supports cancellation
                var supportsCancellation = methods.TryGetValue(executeMethod!, out var executeMethodSyntax) &&
                    executeMethodSyntax.HasCancellationTokenParameter();

                // Extract trigger attributes
                var triggerAttrs = attributes.Where(a => 
                    a.Name.ToString().Contains("ObservableCommandTrigger"));
                
                var commandTypeArguments = member.Type.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.GenericName) ?
                    ((GenericNameSyntax)member.Type).TypeArgumentList.Arguments
                        .Select(a => a.ToString()).ToArray() :
                    [];
                
                var triggers = new List<CommandTriggerInfo>();
                
                foreach (var triggerAttr in triggerAttrs)
                {
                    var triggerPropertyArg = triggerAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString();
                    
                    var triggerTypeArguments = triggerAttr.ChildNodes()
                        .Where(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.GenericName))
                        .SelectMany(t => ((GenericNameSyntax)t).TypeArgumentList.Arguments)
                        .Select(a => a.ToString()).ToArray();

                    if (commandTypeArguments.Length != triggerTypeArguments.Length ||
                        commandTypeArguments.Intersect(triggerTypeArguments).Count() != commandTypeArguments.Length)
                    {
                       // add a diagnostic if the trigger type arguments do not match the command type arguments
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.TriggerTypeArgumentsMismatchError,
                            triggerAttr.GetLocation(),
                            string.Join(", ", commandTypeArguments),
                            string.Join(", ", triggerTypeArguments));
                        diagnostics.Add(diagnostic);
                        continue;
                    }
                    
                    var parameterArg = triggerTypeArguments.Any() ?
                        triggerAttr.ArgumentList?.Arguments.Skip(1).FirstOrDefault()?.Expression.ToString() :
                        null;
                    
                    var canTriggerMethodArg = triggerTypeArguments.Any() ?
                        triggerAttr.ArgumentList?.Arguments.Skip(2).FirstOrDefault()?.Expression.ToString() :
                        triggerAttr.ArgumentList?.Arguments.Skip(1).FirstOrDefault()?.Expression.ToString();
                    
                    // Remove nameof() wrapper and quotes
                    var triggerProperty = triggerPropertyArg?.Replace("nameof(", "").Replace(")", "").Trim('"');
                    var canTriggerMethod = canTriggerMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');
                    
                    if (!string.IsNullOrEmpty(triggerProperty))
                    {
                        // Check for circular references: command method modifies the trigger property
                        if (semanticModel != null && methods.TryGetValue(executeMethod!, out var methodSyntax))
                        {
                            var modifiedProperties = methodSyntax.AnalyzePropertyModifications(semanticModel);
                            if (modifiedProperties.Contains(triggerProperty!))
                            {
                                var diagnostic = Diagnostic.Create(
                                    DiagnosticDescriptors.CircularTriggerReferenceError,
                                    triggerAttr.GetLocation(),
                                    member.Identifier.ValueText, // Command property name
                                    triggerProperty!, // Trigger property name
                                    executeMethod!); // Execute method name
                                diagnostics.Add(diagnostic);
                                continue; // Skip adding this trigger
                            }
                        }
                        
                        triggers.Add(new CommandTriggerInfo(triggerProperty!, canTriggerMethod, parameterArg));
                    }
                }
                
                commandProperties.Add(new CommandPropertyInfo(
                    member.Identifier.ValueText,
                    member.Type!.ToString(),
                    executeMethod!,
                    canExecuteMethod,
                    supportsCancellation,
                    triggers));
            }
        }

        return (commandProperties, diagnostics);;
    }

    public static (List<ModelReferenceInfo> modelReferences, List<Diagnostic> diagnostics) ExtractModelReferencesWithDiagnostics(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel, ServiceInfoList? serviceInfoList = null)
    {
        var modelReferences = new List<ModelReferenceInfo>();
        var diagnostics = new List<Diagnostic>();

        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                
                if (attributeName.StartsWith("ObservableModelReference"))
                {
                    // Extract the generic type parameter with diagnostic information
                    var (referencedModelType, diagnostic) = attribute.ExtractReferencedModelTypeWithDiagnostic(semanticModel, serviceInfoList);
                    
                    if (diagnostic != null)
                    {
                        diagnostics.Add(diagnostic);
                    }
                    
                    if (referencedModelType != null)
                    {
                        var propertyName = referencedModelType.Name; // Simple name as property
                        var usedProperties = classDecl.AnalyzeModelReferenceUsage(referencedModelType.Name);
                        
                        modelReferences.Add(new ModelReferenceInfo(
                            referencedModelType.Name,
                            referencedModelType.ContainingNamespace.ToDisplayString(),
                            propertyName,
                            usedProperties));
                    }
                }
            }
        }

        return (modelReferences, diagnostics);
    }

    public static List<ModelReferenceInfo> ExtractModelReferences(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var (modelReferences, _) = classDecl.ExtractModelReferencesWithDiagnostics(semanticModel);
        return modelReferences;
    }

    public static string ExtractModelScopeFromClass(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var modelScope = "Singleton"; // Default scope

        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                
                if (attributeName.StartsWith("ObservableModelScope") || attributeName == "ObservableModelScope")
                {
                    // Extract the scope parameter
                    var extractedScope = attribute.ExtractModelScope(semanticModel);
                    if (!string.IsNullOrEmpty(extractedScope))
                    {
                        modelScope = extractedScope;
                    }
                }
            }
        }

        return modelScope;
    }
}