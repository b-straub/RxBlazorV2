using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using RxBlazorV2Generator.Helpers;

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
            semanticModel.ThrowIfNull();
            var commandAttr = attributes.FirstOrDefault(a =>
                a.IsObservableCommand(semanticModel));

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
                var formatErrorMethodArg = commandAttr.ArgumentList?.Arguments.Skip(2).FirstOrDefault()?.Expression.ToString();

                // Remove nameof() wrapper and quotes
                var executeMethod = executeMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');
                var canExecuteMethod = canExecuteMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');
                var formatErrorMethod = formatErrorMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');

                semanticModel.ThrowIfNull();
                // Analyze the execute method to determine if it supports cancellation
                var supportsCancellation = executeMethod is not null &&
                                           methods.TryGetValue(executeMethod, out var executeMethodSyntax) &&
                                           executeMethodSyntax.HasCancellationTokenParameter();

                // Validate return value matches command type
                if (executeMethod is not null &&
                    methods.TryGetValue(executeMethod, out var methodDecl))
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

                // Validate formatter method (RXBG091/092). Diagnostic is reported by analyzer; generator skips emission.
                if (!string.IsNullOrEmpty(formatErrorMethod))
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    var formatterArgLocation = commandAttr.ArgumentList?.Arguments.Skip(2).FirstOrDefault()?.GetLocation()
                                               ?? commandAttr.GetLocation();
                    var resolution = classSymbol.ResolveErrorFormatter(formatErrorMethod!, semanticModel.Compilation);

                    if (!resolution.Found)
                    {
                        var props = ImmutableDictionary.CreateBuilder<string, string?>();
                        props.Add("CommandProperty", member.Identifier.ValueText);
                        props.Add("FormatterName", formatErrorMethod);
                        diagnostics.Add(Diagnostic.Create(
                            DiagnosticDescriptors.ErrorFormatterMethodNotFoundError,
                            formatterArgLocation,
                            props.ToImmutable(),
                            member.Identifier.ValueText,
                            formatErrorMethod));
                        // NOTE: Skip code generation - diagnostic is reported by analyzer
                        continue;
                    }

                    if (!resolution.SignatureValid)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            DiagnosticDescriptors.ErrorFormatterMethodInvalidSignatureError,
                            formatterArgLocation,
                            member.Identifier.ValueText,
                            formatErrorMethod,
                            resolution.ActualSignatureDisplay));
                        // NOTE: Skip code generation - diagnostic is reported by analyzer
                        continue;
                    }
                }

                // Extract trigger attributes
                semanticModel.ThrowIfNull();
                var triggerAttrs = attributes.Where(a =>
                    a.IsObservableCommandTrigger(semanticModel));
                
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
                    
                    if (!triggerProperty.IsNullOrEmpty())
                    {
                        // Check for circular references: command method modifies the trigger property
                        if (semanticModel != null && methods.TryGetValue(executeMethod!, out var methodSyntax))
                        {
                            var modifiedProperties = methodSyntax.AnalyzePropertyModifications(semanticModel);
                            if (modifiedProperties.Contains(triggerProperty))
                            {
                                // Pass properties for code fix access
                                var properties = ImmutableDictionary.CreateBuilder<string, string?>();
                                properties.Add("ExecuteMethod", executeMethod);
                                properties.Add("TriggerProperty", triggerProperty);

                                var diagnostic = Diagnostic.Create(
                                    DiagnosticDescriptors.CircularTriggerReferenceError,
                                    triggerAttr.GetLocation(),
                                    properties.ToImmutable(),
                                    member.Identifier.ValueText, // Command property name
                                    triggerProperty, // Trigger property name
                                    executeMethod); // Execute method name
                                diagnostics.Add(diagnostic);
                                continue; // Skip adding this trigger
                            }
                        }
                        
                        triggers.Add(new CommandTriggerInfo(triggerProperty, canTriggerMethod, parameterArg));
                    }
                }

                // Transfer triggers from abstract base class when this is an override
                var isOverride = member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.OverrideKeyword));
                if (isOverride)
                {
                    semanticModel.ThrowIfNull();
                    var propertySymbol = semanticModel.GetDeclaredSymbol(member) as IPropertySymbol;
                    var baseProperty = propertySymbol?.OverriddenProperty;
                    if (baseProperty?.IsAbstract == true)
                    {
                        foreach (var attr in baseProperty.GetAttributes())
                        {
                            var attrName = attr.AttributeClass?.Name;
                            if (attrName == "ObservableCommandTriggerAttribute")
                            {
                                // Extract trigger property from base attribute
                                var baseTriggerProperty = attr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                                if (!string.IsNullOrEmpty(baseTriggerProperty) &&
                                    !triggers.Any(t => t.TriggerProperty == baseTriggerProperty))
                                {
                                    // Extract optional canTriggerMethod (second argument)
                                    string? baseCanTriggerMethod = null;
                                    if (attr.ConstructorArguments.Length > 1)
                                    {
                                        baseCanTriggerMethod = attr.ConstructorArguments[1].Value?.ToString();
                                    }

                                    triggers.Add(new CommandTriggerInfo(baseTriggerProperty!, baseCanTriggerMethod));
                                }
                            }
                        }
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
                    member.Type.ToString(),
                    executeMethod!,
                    canExecuteMethod,
                    supportsCancellation,
                    isOverride,
                    triggers,
                    accessibility,
                    formatErrorMethod));
            }
        }

        return (commandProperties, diagnostics);;
    }


    /// <summary>
    /// Result of resolving a per-command error formatter method by name.
    /// </summary>
    public readonly record struct ErrorFormatterResolution(bool Found, bool SignatureValid, string ActualSignatureDisplay);

    /// <summary>
    /// Walks the type and its bases looking for a method with the given name.
    /// Reports whether a candidate exists and, if so, whether at least one overload matches
    /// <c>string Method(System.Exception)</c> (instance or static, any accessibility).
    /// </summary>
    public static ErrorFormatterResolution ResolveErrorFormatter(this INamedTypeSymbol? typeSymbol, string methodName, Compilation compilation)
    {
        if (typeSymbol is null)
        {
            return new ErrorFormatterResolution(false, false, "");
        }

        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType is null)
        {
            // Should never happen in a well-formed compilation; fail open so we don't block codegen.
            return new ErrorFormatterResolution(true, true, "");
        }

        IMethodSymbol? firstFound = null;
        for (var current = typeSymbol; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(methodName))
            {
                if (member is IMethodSymbol method)
                {
                    firstFound ??= method;
                    if (SymbolEqualityComparer.Default.Equals(method.ReturnType, stringType) &&
                        method.Parameters.Length == 1 &&
                        SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, exceptionType))
                    {
                        return new ErrorFormatterResolution(true, true, "");
                    }
                }
            }
        }

        if (firstFound is null)
        {
            return new ErrorFormatterResolution(false, false, "");
        }

        var paramList = string.Join(", ", firstFound.Parameters.Select(p => p.Type.ToDisplayString()));
        var actual = $"{firstFound.ReturnType.ToDisplayString()} {firstFound.Name}({paramList})";
        return new ErrorFormatterResolution(true, false, actual);
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