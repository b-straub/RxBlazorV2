using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Extensions;

public static class PropertyAnalysisExtensions
{
    public static (List<PartialPropertyInfo>, List<Diagnostic>) ExtractPartialPropertiesWithDiagnostics(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        if (semanticModel is null)
        {
            throw new ArgumentNullException(nameof(semanticModel), "SemanticModel cannot be null for property analysis");
        }

        var partialProperties = new List<PartialPropertyInfo>();
        var diagnostics = new List<Diagnostic>();

        foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (member.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                member.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true)
            {
                var hasSetAccessor = member.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) == true;
                var hasInitAccessor = member.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)) == true;

                if (!hasSetAccessor && !hasInitAccessor)
                {
                    continue;
                }

                var hasRequiredModifier = member.Modifiers.Any(SyntaxKind.RequiredKeyword);
                var isObservableCollection = member.IsObservableCollectionProperty(semanticModel);
                var isEquatable = member.IsEquatableProperty(semanticModel);
                var batchIds = member.GetObservableBatchIds(semanticModel);

                // Validate init accessor - only allowed for IObservableCollection
                if (hasInitAccessor && !isObservableCollection)
                {
                    var diagnostic = Diagnostic.Create(
                        Diagnostics.DiagnosticDescriptors.InvalidInitPropertyError,
                        member.Identifier.GetLocation(),
                        member.Identifier.ValueText,
                        member.Type!.ToString());
                    diagnostics.Add(diagnostic);
                }

                // Extract accessibility modifier
                var accessibility = member.Modifiers
                    .Where(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                m.IsKind(SyntaxKind.PrivateKeyword) ||
                                m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                m.IsKind(SyntaxKind.InternalKeyword))
                    .Select(m => m.ValueText)
                    .FirstOrDefault() ?? "public";

                // Extract ObservableTrigger and ObservableTriggerAsync attributes
                var attributes = member.AttributeLists.SelectMany(al => al.Attributes).ToArray();
                var syncTriggerAttrs = attributes.Where(a => a.IsObservableTrigger(semanticModel));
                var asyncTriggerAttrs = attributes.Where(a => a.IsObservableTriggerAsync(semanticModel));
                var triggers = new List<PropertyTriggerInfo>();

                // Process sync triggers
                foreach (var triggerAttr in syncTriggerAttrs)
                {
                    var trigger = ExtractTriggerInfo(triggerAttr, member, classDecl, semanticModel, isAsync: false, diagnostics);
                    if (trigger is not null)
                    {
                        triggers.Add(trigger);
                    }
                }

                // Process async triggers
                foreach (var triggerAttr in asyncTriggerAttrs)
                {
                    var trigger = ExtractTriggerInfo(triggerAttr, member, classDecl, semanticModel, isAsync: true, diagnostics);
                    if (trigger is not null)
                    {
                        triggers.Add(trigger);
                    }
                }

                // Extract ObservableCallbackTrigger attributes
                var callbackTriggers = new List<CallbackTriggerInfo>();

                var syncCallbackAttr = attributes.FirstOrDefault(a => a.IsObservableCallbackTrigger(semanticModel));
                if (syncCallbackAttr is not null)
                {
                    var methodNameArg = syncCallbackAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString();
                    var methodName = methodNameArg?.Replace("nameof(", "").Replace(")", "").Trim('"');

                    if (string.IsNullOrEmpty(methodName))
                    {
                        methodName = $"On{member.Identifier.ValueText}Changed";
                    }

                    if (methodName is null)
                    {
                        throw new InvalidOperationException($"Failed to determine callback trigger method name for property {member.Identifier.ValueText}");
                    }

                    callbackTriggers.Add(new CallbackTriggerInfo(methodName, CallbackTriggerType.Sync));
                }

                var asyncCallbackAttr = attributes.FirstOrDefault(a => a.IsObservableCallbackTriggerAsync(semanticModel));
                if (asyncCallbackAttr is not null)
                {
                    var methodNameArg = asyncCallbackAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString();
                    var methodName = methodNameArg?.Replace("nameof(", "").Replace(")", "").Trim('"');

                    if (string.IsNullOrEmpty(methodName))
                    {
                        methodName = $"On{member.Identifier.ValueText}ChangedAsync";
                    }

                    if (methodName is null)
                    {
                        throw new InvalidOperationException($"Failed to determine callback trigger method name for property {member.Identifier.ValueText}");
                    }

                    callbackTriggers.Add(new CallbackTriggerInfo(methodName, CallbackTriggerType.Async));
                }

                partialProperties.Add(new PartialPropertyInfo(
                    member.Identifier.ValueText,
                    member.Type!.ToString(),
                    isObservableCollection,
                    isEquatable,
                    batchIds,
                    hasRequiredModifier,
                    hasInitAccessor,
                    accessibility,
                    triggers,
                    callbackTriggers));
            }
        }

        return (partialProperties, diagnostics);
    }

    public static List<PartialPropertyInfo> ExtractPartialProperties(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var (properties, _) = ExtractPartialPropertiesWithDiagnostics(classDecl, semanticModel);
        return properties;
    }

    private static PropertyTriggerInfo? ExtractTriggerInfo(
        AttributeSyntax triggerAttr,
        PropertyDeclarationSyntax member,
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        bool isAsync,
        List<Diagnostic> diagnostics)
    {
        var executeMethodArg = triggerAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString();

        // Check for generic trigger (with parameter)
        var triggerTypeArguments = triggerAttr.ChildNodes()
            .Where(t => t.IsKind(SyntaxKind.GenericName))
            .SelectMany(t => ((GenericNameSyntax)t).TypeArgumentList.Arguments)
            .Select(a => a.ToString()).ToArray();

        var parameterArg = triggerTypeArguments.Any() ?
            triggerAttr.ArgumentList?.Arguments.Skip(1).FirstOrDefault()?.Expression.ToString() :
            null;

        var canTriggerMethodArg = triggerTypeArguments.Any() ?
            triggerAttr.ArgumentList?.Arguments.Skip(2).FirstOrDefault()?.Expression.ToString() :
            triggerAttr.ArgumentList?.Arguments.Skip(1).FirstOrDefault()?.Expression.ToString();

        // Remove nameof() wrapper and quotes
        var executeMethod = executeMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');
        var canTriggerMethod = canTriggerMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');

        if (string.IsNullOrEmpty(executeMethod))
        {
            return null;
        }

        // Explicit null check for flow analysis (should never be null after above check)
        if (executeMethod is null)
        {
            throw new InvalidOperationException($"Execute method name was unexpectedly null for trigger on property {member.Identifier.ValueText}");
        }

        // Analyze the execute method to determine if it supports cancellation
        var supportsCancellation = classDecl.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == executeMethod)
            ?.HasCancellationTokenParameter(semanticModel) ?? false;

        // Check for circular references: trigger method modifies the property it's attached to
        var methodSyntax = classDecl.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ValueText == executeMethod);

        if (methodSyntax is not null)
        {
            var modifiedProperties = methodSyntax.AnalyzePropertyModifications(semanticModel);
            if (modifiedProperties.Contains(member.Identifier.ValueText))
            {
                var diagnostic = Diagnostic.Create(
                    Diagnostics.DiagnosticDescriptors.CircularTriggerReferenceError,
                    triggerAttr.GetLocation(),
                    member.Identifier.ValueText, // Property name
                    member.Identifier.ValueText, // Trigger property (same as property for ObservableTrigger)
                    executeMethod); // Execute method name
                diagnostics.Add(diagnostic);
                return null; // Skip this trigger
            }
        }

        var trigger = new PropertyTriggerInfo(executeMethod, canTriggerMethod, parameterArg, supportsCancellation, isAsync)
        {
            PropertyName = member.Identifier.ValueText
        };
        return trigger;
    }

    public static string[]? GetObservableBatchIds(this PropertyDeclarationSyntax property, SemanticModel semanticModel)
    {
        try
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            if (propertySymbol is not IPropertySymbol propSymbol)
            {
                return null;
            }

            var batchIds = new List<string>();
            foreach (var attribute in propSymbol.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass?.Name == "ObservableBatchAttribute" &&
                    attribute.ConstructorArguments.Length > 0)
                {
                    var batchIdArg = attribute.ConstructorArguments[0];
                    if (batchIdArg.Value is string batchId)
                    {
                        batchIds.Add(batchId);
                    }
                }
            }

            return batchIds.Count > 0 ? batchIds.ToArray() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static List<string> AnalyzeMethodForPropertyUsage(this Dictionary<string, MethodDeclarationSyntax> methods,
        string methodName,
        ObservableModelInfo modelInfo)
    {
        try
        {
            if (!methods.TryGetValue(methodName, out var method))
            {
                return new List<string>();
            }

            var usedProperties = new HashSet<string>();
            var partialPropertyNames = new HashSet<string>(modelInfo.PartialProperties.Select(p => p.Name));

            // Walk through the method body and find property references
            var descendants = method.DescendantNodes();
            foreach (var node in descendants)
            {
                if (node is IdentifierNameSyntax identifier)
                {
                    var identifierName = identifier.Identifier.ValueText;
                    if (partialPropertyNames.Contains(identifierName))
                    {
                        usedProperties.Add(identifierName);
                    }
                }
                else if (node is MemberAccessExpressionSyntax memberAccess)
                {
                    var memberName = memberAccess.Name.Identifier.ValueText;
                    if (partialPropertyNames.Contains(memberName))
                    {
                        usedProperties.Add(memberName);
                    }
                }
            }

            return usedProperties.ToList();
        }
        catch (Exception)
        {
            // Return empty list on analysis error rather than throwing
            return new List<string>();
        }
    }

    public static List<string> AnalyzeMethodForPropertyModifications(this Dictionary<string, MethodDeclarationSyntax> methods,
        string methodName,
        ObservableModelInfo modelInfo)
    {
        try
        {
            if (!methods.TryGetValue(methodName, out var method))
            {
                return new List<string>();
            }

            var modifiedProperties = new HashSet<string>();
            var partialPropertyNames = new HashSet<string>(modelInfo.PartialProperties.Select(p => p.Name));

            // Walk through the method body and find property modifications (syntactic analysis only)
            var descendants = method.DescendantNodes();
            foreach (var node in descendants)
            {
                // Look for assignment expressions: property = value
                if (node is AssignmentExpressionSyntax assignment)
                {
                    var leftSide = assignment.Left;

                    // Handle direct property assignment: MyProperty = value
                    if (leftSide is IdentifierNameSyntax identifier)
                    {
                        var identifierName = identifier.Identifier.ValueText;
                        if (partialPropertyNames.Contains(identifierName))
                        {
                            modifiedProperties.Add(identifierName);
                        }
                    }
                    // Handle member access: this.MyProperty = value
                    else if (leftSide is MemberAccessExpressionSyntax memberAccess)
                    {
                        var memberName = memberAccess.Name.Identifier.ValueText;
                        if (partialPropertyNames.Contains(memberName))
                        {
                            modifiedProperties.Add(memberName);
                        }
                    }
                }
                // Look for increment/decrement operations: MyProperty++, MyProperty--, ++MyProperty, --MyProperty
                else if (node is PostfixUnaryExpressionSyntax postfixUnary &&
                         (postfixUnary.IsKind(SyntaxKind.PostIncrementExpression) ||
                          postfixUnary.IsKind(SyntaxKind.PostDecrementExpression)))
                {
                    if (postfixUnary.Operand is IdentifierNameSyntax identifier)
                    {
                        var identifierName = identifier.Identifier.ValueText;
                        if (partialPropertyNames.Contains(identifierName))
                        {
                            modifiedProperties.Add(identifierName);
                        }
                    }
                    else if (postfixUnary.Operand is MemberAccessExpressionSyntax memberAccess)
                    {
                        var memberName = memberAccess.Name.Identifier.ValueText;
                        if (partialPropertyNames.Contains(memberName))
                        {
                            modifiedProperties.Add(memberName);
                        }
                    }
                }
                else if (node is PrefixUnaryExpressionSyntax prefixUnary &&
                         (prefixUnary.IsKind(SyntaxKind.PreIncrementExpression) ||
                          prefixUnary.IsKind(SyntaxKind.PreDecrementExpression)))
                {
                    if (prefixUnary.Operand is IdentifierNameSyntax identifier)
                    {
                        var identifierName = identifier.Identifier.ValueText;
                        if (partialPropertyNames.Contains(identifierName))
                        {
                            modifiedProperties.Add(identifierName);
                        }
                    }
                    else if (prefixUnary.Operand is MemberAccessExpressionSyntax memberAccess)
                    {
                        var memberName = memberAccess.Name.Identifier.ValueText;
                        if (partialPropertyNames.Contains(memberName))
                        {
                            modifiedProperties.Add(memberName);
                        }
                    }
                }
            }

            return modifiedProperties.ToList();
        }
        catch (Exception)
        {
            // Return empty list on analysis error rather than throwing
            return new List<string>();
        }
    }

    public static List<string> GetObservedPropertiesForCommand(this ObservableModelInfo modelInfo, CommandPropertyInfo command)
    {
        var observedProps = new HashSet<string>();

        // Get the trigger property names for this command
        var triggerPropertyNames = new HashSet<string>(
            command.Triggers.Select(t => t.TriggerProperty));

        // Analyze execute method for property MODIFICATIONS
        if (command.ExecuteMethod != null && modelInfo.Methods.TryGetValue(command.ExecuteMethod, out var executeMethod))
        {
            var modifiedProps = modelInfo.Methods.AnalyzeMethodForPropertyModifications(command.ExecuteMethod, modelInfo);
            foreach (var prop in modifiedProps)
                observedProps.Add(prop);
        }

        // Analyze canExecute method for property dependencies
        if (command.CanExecuteMethod != null)
        {
            var canExecuteProps = modelInfo.Methods.AnalyzeMethodForPropertyUsage(command.CanExecuteMethod, modelInfo);
            foreach (var prop in canExecuteProps)
                observedProps.Add(prop);
        }

        // CRITICAL: Remove trigger properties from _observedProperties if they're not modified
        // This prevents circular triggers: when the command completes, StateHasChanged(_observedProperties)
        // would notify about trigger properties, causing the command to re-trigger itself
        var modifiedPropsInExecute = command.ExecuteMethod != null && modelInfo.Methods.TryGetValue(command.ExecuteMethod, out var execMethod)
            ? new HashSet<string>(modelInfo.Methods.AnalyzeMethodForPropertyModifications(command.ExecuteMethod, modelInfo))
            : new HashSet<string>();

        foreach (var triggerProp in triggerPropertyNames)
        {
            // Only keep trigger property in _observedProperties if it's actually modified
            if (!modifiedPropsInExecute.Contains(triggerProp))
            {
                observedProps.Remove(triggerProp);
            }
        }

        // Prefix all properties with "Model." for component context
        return observedProps.Select(p => $"Model.{p}").ToList();
    }

    public static List<string> AnalyzePropertyModifications(this MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        var modifiedProperties = new HashSet<string>();

        try
        {
            // Walk through the method body and find property assignments
            var descendants = method.DescendantNodes();
            foreach (var node in descendants)
            {
                // Look for assignment expressions: property = value
                if (node is AssignmentExpressionSyntax assignment)
                {
                    var leftSide = assignment.Left;

                    // Handle direct property assignment: MyProperty = value
                    if (leftSide is IdentifierNameSyntax identifier)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(identifier.Identifier.ValueText);
                        }
                    }
                    // Handle member access: this.MyProperty = value or model.MyProperty = value
                    else if (leftSide is MemberAccessExpressionSyntax memberAccess)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            // Check if this is a referenced model property (e.g., ModelReferencesShared.NotificationsEnabled)
                            var qualifiedName = ExtractQualifiedPropertyName(memberAccess, semanticModel);
                            modifiedProperties.Add(qualifiedName);
                        }
                    }
                }
                // Look for increment/decrement operations: MyProperty++, MyProperty--, ++MyProperty, --MyProperty
                else if (node is PostfixUnaryExpressionSyntax postfixUnary &&
                         (postfixUnary.IsKind(SyntaxKind.PostIncrementExpression) ||
                          postfixUnary.IsKind(SyntaxKind.PostDecrementExpression)))
                {
                    if (postfixUnary.Operand is IdentifierNameSyntax identifier)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(identifier.Identifier.ValueText);
                        }
                    }
                    else if (postfixUnary.Operand is MemberAccessExpressionSyntax memberAccess)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            // Check if this is a referenced model property
                            var qualifiedName = ExtractQualifiedPropertyName(memberAccess, semanticModel);
                            modifiedProperties.Add(qualifiedName);
                        }
                    }
                }
                else if (node is PrefixUnaryExpressionSyntax prefixUnary &&
                         (prefixUnary.IsKind(SyntaxKind.PreIncrementExpression) ||
                          prefixUnary.IsKind(SyntaxKind.PreDecrementExpression)))
                {
                    if (prefixUnary.Operand is IdentifierNameSyntax identifier)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(identifier);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            modifiedProperties.Add(identifier.Identifier.ValueText);
                        }
                    }
                    else if (prefixUnary.Operand is MemberAccessExpressionSyntax memberAccess)
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
                        if (symbolInfo.Symbol is IPropertySymbol)
                        {
                            // Check if this is a referenced model property
                            var qualifiedName = ExtractQualifiedPropertyName(memberAccess, semanticModel);
                            modifiedProperties.Add(qualifiedName);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Return empty list on analysis error rather than throwing
        }

        return modifiedProperties.ToList();
    }

    /// <summary>
    /// Extracts the qualified property name from a member access expression.
    /// For direct property access (this.MyProperty), returns just the property name.
    /// For referenced model property access (Model.Property), returns "Model.Property".
    /// </summary>
    private static string ExtractQualifiedPropertyName(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel)
    {
        var propertyName = memberAccess.Name.Identifier.ValueText;

        // Check if the expression is accessing a property through another property/field
        // e.g., ModelReferencesShared.NotificationsEnabled
        if (memberAccess.Expression is IdentifierNameSyntax identifierExpression)
        {
            var expressionSymbol = semanticModel.GetSymbolInfo(identifierExpression).Symbol;

            // If the left side is a property or field (not 'this'), return qualified name
            if (expressionSymbol is IPropertySymbol or IFieldSymbol)
            {
                return $"{identifierExpression.Identifier.ValueText}.{propertyName}";
            }
        }

        // For 'this.Property' or direct access, just return the property name
        return propertyName;
    }

    public static bool IsObservableCollectionProperty(this PropertyDeclarationSyntax property, SemanticModel semanticModel)
    {
        try
        {
            // Get the type symbol for the property
            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            if (propertySymbol is not IPropertySymbol propSymbol) 
                return false;

            var propertyType = propSymbol.Type;
            
            // Check if the type implements IObservableCollection<T>
            return ImplementsIObservableCollection(propertyType);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool ImplementsIObservableCollection(ITypeSymbol typeSymbol)
    {
        // Check all interfaces implemented by this type
        foreach (var interfaceType in typeSymbol.AllInterfaces)
        {
            // Check for IObservableCollection<T> from ObservableCollections namespace
            if (interfaceType.Name == "IObservableCollection" &&
                interfaceType.ContainingNamespace?.ToDisplayString() == "ObservableCollections")
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsEquatableProperty(this PropertyDeclarationSyntax property, SemanticModel semanticModel)
    {
        try
        {
            // Get the type symbol for the property
            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            if (propertySymbol is not IPropertySymbol propSymbol)
            {
                return false;
            }

            var propertyType = propSymbol.Type;

            // Check if the type is equatable
            return IsEquatableType(propertyType);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsEquatableType(ITypeSymbol type)
    {
        // Don't add equality check for generic type parameters without constraints
        // because we don't know if they support the != operator
        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return false;
        }

        // Handle nullable types
        type = GetUnderlyingNullableTypeOrSelf(type);

        // Check for built-in special types (primitives, string, object, etc.)
        if (type.SpecialType is not SpecialType.None)
        {
            return true;
        }
        
        // Check if the type implements IEquatable<T>
        foreach (var interfaceType in type.AllInterfaces)
        {
            if (interfaceType.OriginalDefinition.ToDisplayString() == "System.IEquatable<T>")
            {
                return true;
            }
        }

        // Check if it's a record or record struct (they implement IEquatable<T> automatically)
        if (type is INamedTypeSymbol { IsRecord: true })
        {
            return true;
        }

        // Check if it's an enum (enums are always equatable)
        return type.TypeKind == TypeKind.Enum;
    }

    private static ITypeSymbol GetUnderlyingNullableTypeOrSelf(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
                namedTypeSymbol.TypeArguments.Length == 1)
            {
                return namedTypeSymbol.TypeArguments[0];
            }
        }

        return typeSymbol;
    }
}