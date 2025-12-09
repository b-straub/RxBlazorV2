using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Diagnostics;

namespace RxBlazorV2Generator.Extensions;

public static class AttributeAnalysisExtensions
{

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

    public static (List<CommandPropertyInfo> commandProperties, List<Diagnostic> diagnostics) ExtractCommandPropertiesWithDiagnostics(this ClassDeclarationSyntax classDecl, Dictionary<string, MethodDeclarationSyntax> methods, SemanticModel semanticModel)
    {
        var commandProperties = new List<CommandPropertyInfo>();
        var diagnostics = new List<Diagnostic>();

        foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            var attributes = member.AttributeLists.SelectMany(al => al.Attributes).ToArray();
            var commandAttr = attributes.FirstOrDefault(a =>
                a.IsObservableCommand(semanticModel!));

            if (commandAttr != null)
            {
                // Check if command property is missing 'partial' modifier (RXBG072)
                var isPartial = member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));
                if (!isPartial)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.ObservableEntityMissingPartialModifierError,
                        member.Identifier.GetLocation(),
                        "Property",
                        member.Identifier.ValueText,
                        "implements IObservableCommand",
                        "property");
                    diagnostics.Add(diagnostic);
                    // NOTE: Skip code generation for non-partial command properties
                    continue;
                }

                var executeMethodArg = commandAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString();
                var canExecuteMethodArg = commandAttr.ArgumentList?.Arguments.Skip(1).FirstOrDefault()?.Expression.ToString();

                // Remove nameof() wrapper and quotes
                var executeMethod = executeMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');
                var canExecuteMethod = canExecuteMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');

                // Analyze the execute method to determine if it supports cancellation
                var supportsCancellation = executeMethod is not null &&
                                           methods.TryGetValue(executeMethod, out var executeMethodSyntax) &&
                                           executeMethodSyntax.HasCancellationTokenParameter(semanticModel!);

                // Validate return value matches command type
                if (executeMethod is not null &&
                    methods.TryGetValue(executeMethod, out var methodDecl) &&
                    semanticModel is not null &&
                    member.Type is not null)
                {
                    var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
                    var commandTypeString = member.Type.ToString();
                    var commandTypeLocation = member.Type.GetLocation();

                    if (methodSymbol is not null)
                    {
                        var returnType = methodSymbol.ReturnType;
                        var isAsync = returnType.Name == "Task";
                        var actualReturnType = isAsync && returnType is INamedTypeSymbol namedReturnType && namedReturnType.TypeArguments.Length > 0
                            ? namedReturnType.TypeArguments[0]
                            : returnType;

                        var hasReturnValue = !returnType.SpecialType.Equals(SpecialType.System_Void) &&
                                            !(isAsync && returnType is INamedTypeSymbol taskType && taskType.TypeArguments.Length == 0);

                        var commandExpectsReturn = commandTypeString.Contains("IObservableCommandR");
                        var commandProhibitsReturn = !commandExpectsReturn;

                        // RXBG032: IObservableCommand/IObservableCommandAsync should not have return values
                        if (commandProhibitsReturn && hasReturnValue)
                        {
                            var actualReturnTypeName = actualReturnType.ToDisplayString();
                            var diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.CommandMethodReturnsValueError,
                                commandTypeLocation,
                                member.Identifier.ValueText,
                                commandTypeString,
                                executeMethod,
                                actualReturnTypeName);
                            diagnostics.Add(diagnostic);
                            // NOTE: Skip code generation - diagnostic is reported by analyzer
                            continue;
                        }

                        // RXBG033: IObservableCommandR/IObservableCommandRAsync must have return values
                        if (commandExpectsReturn && !hasReturnValue)
                        {
                            // Extract expected return type from command type
                            var expectedReturnType = commandTypeString.Contains("<") ?
                                commandTypeString.Substring(commandTypeString.LastIndexOf('<') + 1).TrimEnd('>') :
                                "unknown";

                            var actualReturnTypeName = isAsync ? "Task" : "void";
                            var diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.CommandMethodMissingReturnValueError,
                                commandTypeLocation,
                                member.Identifier.ValueText,
                                commandTypeString,
                                expectedReturnType,
                                executeMethod,
                                actualReturnTypeName);
                            diagnostics.Add(diagnostic);
                            // NOTE: Skip code generation - diagnostic is reported by analyzer
                            continue;
                        }
                    }
                }

                // Extract trigger attributes
                var triggerAttrs = attributes.Where(a =>
                    a.IsObservableCommandTrigger(semanticModel!));
                
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

                // Extract accessibility modifier
                var accessibility = member.Modifiers
                    .Where(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword) ||
                                m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword) ||
                                m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ProtectedKeyword) ||
                                m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.InternalKeyword))
                    .Select(m => m.ValueText)
                    .FirstOrDefault() ?? "public";

                commandProperties.Add(new CommandPropertyInfo(
                    member.Identifier.ValueText,
                    member.Type!.ToString(),
                    executeMethod!,
                    canExecuteMethod,
                    supportsCancellation,
                    triggers,
                    accessibility));
            }
        }

        return (commandProperties, diagnostics);;
    }


    public static string ExtractModelScopeFromClass(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var modelScope = "Scoped"; // Default scope

        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.IsObservableModelScope(semanticModel))
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

    public static (string scope, bool hasAttribute) ExtractModelScopeWithAttributeCheck(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var modelScope = "Scoped"; // Default scope
        var hasAttribute = false;

        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.IsObservableModelScope(semanticModel))
                {
                    hasAttribute = true;
                    // Extract the scope parameter
                    var extractedScope = attribute.ExtractModelScope(semanticModel);
                    if (!string.IsNullOrEmpty(extractedScope))
                    {
                        modelScope = extractedScope;
                    }
                }
            }
        }

        return (modelScope, hasAttribute);
    }
}