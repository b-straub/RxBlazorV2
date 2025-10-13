using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Extensions;

public static class PropertyAnalysisExtensions
{
    public static (List<PartialPropertyInfo>, List<Diagnostic>) ExtractPartialPropertiesWithDiagnostics(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
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
                        RxBlazorV2Generator.Diagnostics.DiagnosticDescriptors.InvalidInitPropertyError,
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

                partialProperties.Add(new PartialPropertyInfo(
                    member.Identifier.ValueText,
                    member.Type!.ToString(),
                    isObservableCollection,
                    isEquatable,
                    batchIds,
                    hasRequiredModifier,
                    hasInitAccessor,
                    accessibility));
            }
        }

        return (partialProperties, diagnostics);
    }

    public static List<PartialPropertyInfo> ExtractPartialProperties(this ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var (properties, _) = ExtractPartialPropertiesWithDiagnostics(classDecl, semanticModel);
        return properties;
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

        return observedProps.ToList();
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
                            modifiedProperties.Add(memberAccess.Name.Identifier.ValueText);
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
                            modifiedProperties.Add(memberAccess.Name.Identifier.ValueText);
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
                            modifiedProperties.Add(memberAccess.Name.Identifier.ValueText);
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