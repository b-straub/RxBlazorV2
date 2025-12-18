using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Extensions;

public static class MethodAnalysisExtensions
{
    public static Dictionary<string, MethodDeclarationSyntax> CollectMethods(this ClassDeclarationSyntax classDecl)
    {
        var methods = new Dictionary<string, MethodDeclarationSyntax>();

        foreach (var member in classDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            methods[member.Identifier.ValueText] = member;
        }

        return methods;
    }

    public static bool HasCancellationTokenParameter(
        this MethodDeclarationSyntax method,
        SemanticModel semanticModel)
    {
        foreach (var param in method.ParameterList.Parameters)
        {
            if (param.Type == null)
            {
                continue;
            }

            var typeInfo = semanticModel.GetTypeInfo(param.Type);
            if (typeInfo.Type != null &&
                typeInfo.Type.Name == "CancellationToken" &&
                typeInfo.Type.ContainingNamespace?.ToDisplayString() == "System.Threading")
            {
                return true;
            }
        }
        return false;
    }

    public static List<string> AnalyzeMethodForModelReferences(
        this MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        ITypeSymbol referencedModelType)
    {
        var usedProperties = new HashSet<string>();

        // Use semantic model to find property access patterns
        foreach (var node in method.DescendantNodes())
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
                var symbol = symbolInfo.Symbol;

                if (symbol == null)
                {
                    continue;
                }

                ITypeSymbol? expressionType = null;

                // Handle property access (property is of the model type)
                if (symbol is IPropertySymbol propertySymbol)
                {
                    expressionType = propertySymbol.Type;
                }
                // Handle local variable
                else if (symbol is ILocalSymbol localSymbol)
                {
                    expressionType = localSymbol.Type;
                }
                // Handle parameter
                else if (symbol is IParameterSymbol paramSymbol)
                {
                    expressionType = paramSymbol.Type;
                }
                // Handle field
                else if (symbol is IFieldSymbol fieldSymbol)
                {
                    expressionType = fieldSymbol.Type;
                }

                // Check if the expression type matches the referenced model type
                if (expressionType != null &&
                    SymbolEqualityComparer.Default.Equals(expressionType, referencedModelType))
                {
                    usedProperties.Add(memberAccess.Name.Identifier.ValueText);
                }
            }
        }

        return usedProperties.ToList();
    }

    /// <summary>
    /// Analyzes private methods in the class for referenced model property access.
    /// Returns a list of InternalModelObserverInfo for methods that:
    /// - Are private
    /// - Have no parameters (or only CancellationToken for async)
    /// - Access properties from referenced models
    /// - Return void (sync) or Task/ValueTask (async)
    /// - Are NOT already used as command execute/canExecute methods or property trigger methods
    /// </summary>
    public static List<InternalModelObserverInfo> AnalyzeInternalModelObservers(
        this ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        List<ModelReferenceInfo> modelReferences,
        Dictionary<string, ITypeSymbol> modelSymbols,
        HashSet<string>? excludedMethods = null)
    {
        var (observers, _) = AnalyzeInternalModelObserversWithDiagnostics(classDecl, semanticModel, modelReferences, modelSymbols, excludedMethods);
        return observers;
    }

    /// <summary>
    /// Analyzes methods in the class for referenced model property access.
    /// Returns both valid observers and information about methods with invalid signatures.
    /// Methods in excludedMethods are skipped (e.g., command execute/canExecute methods, trigger methods).
    /// </summary>
    public static (List<InternalModelObserverInfo> Observers, List<InvalidInternalModelObserverInfo> InvalidObservers) AnalyzeInternalModelObserversWithDiagnostics(
        this ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        List<ModelReferenceInfo> modelReferences,
        Dictionary<string, ITypeSymbol> modelSymbols,
        HashSet<string>? excludedMethods = null)
    {
        var observers = new List<InternalModelObserverInfo>();
        var invalidObservers = new List<InvalidInternalModelObserverInfo>();

        if (modelReferences.Count == 0)
        {
            return (observers, invalidObservers);
        }

        // Build a map of property names to their owning model reference
        // Key: property name (e.g., "AutoRefresh"), Value: model reference info
        var propertyToModelRef = new Dictionary<string, (ModelReferenceInfo modelRef, string propertyName)>();
        foreach (var modelRef in modelReferences)
        {
            if (!modelSymbols.TryGetValue(modelRef.PropertyName, out var modelSymbol))
            {
                continue;
            }

            // Get all property names from the referenced model type
            foreach (var member in modelSymbol.GetMembers())
            {
                if (member is IPropertySymbol propertySymbol)
                {
                    // Only add if not already present (first model wins if multiple models have same property name)
                    if (!propertyToModelRef.ContainsKey(propertySymbol.Name))
                    {
                        propertyToModelRef[propertySymbol.Name] = (modelRef, propertySymbol.Name);
                    }
                }
            }
        }

        foreach (var member in classDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            var methodName = member.Identifier.ValueText;

            // Skip methods that are already used as command execute/canExecute or trigger methods
            // These methods should not be auto-detected as internal observers
            if (excludedMethods is not null && excludedMethods.Contains(methodName))
            {
                continue;
            }

            // First, find which model properties this method accesses
            var propertiesByModelRef = FindAccessedModelProperties(member, modelReferences, propertyToModelRef);

            // If no model properties accessed, skip this method entirely
            if (!propertiesByModelRef.Any(kvp => kvp.Value.Count > 0))
            {
                continue;
            }

            // Check if this is a valid internal observer
            var isPrivate = member.Modifiers.Any(SyntaxKind.PrivateKeyword);
            var (isValidSignature, isAsync, hasCancellationToken, invalidReason) = ValidateObserverMethodSignatureWithReason(member, semanticModel);

            if (isPrivate && isValidSignature)
            {
                // Check for circular references: method modifies a property it observes
                // Pass known model reference names for pattern matching (generated properties aren't in semantic model)
                var knownModelRefNames = new HashSet<string>(modelReferences.Select(mr => mr.PropertyName));
                var modificationLocations = member.AnalyzePropertyModificationsWithLocations(semanticModel, knownModelRefNames);
                var modifiedProperties = new HashSet<string>(modificationLocations.Keys);

                // Valid internal observer
                foreach (var kvp in propertiesByModelRef)
                {
                    var modelRefName = kvp.Key;
                    var usedProperties = kvp.Value;

                    if (usedProperties.Count > 0)
                    {
                        // Check if any modified property matches an observed property
                        var circularProperties = usedProperties
                            .Where(prop => modifiedProperties.Contains($"{modelRefName}.{prop}"))
                            .ToList();

                        if (circularProperties.Count > 0)
                        {
                            // Circular reference detected - report as invalid
                            // Get the location of the first circular property modification
                            var firstCircularProp = $"{modelRefName}.{circularProperties[0]}";
                            var modificationLocation = modificationLocations.TryGetValue(firstCircularProp, out var loc) ? loc : null;

                            var circularPropList = string.Join(", ", circularProperties);
                            invalidObservers.Add(new InvalidInternalModelObserverInfo(
                                methodName,
                                modelRefName,
                                usedProperties,
                                $"Circular reference: method modifies observed property '{circularPropList}' on '{modelRefName}'. This creates an infinite loop.",
                                member.Identifier.GetLocation(),
                                isCircularReference: true,
                                circularProperties: circularProperties,
                                modificationLocation: modificationLocation));
                        }
                        else
                        {
                            observers.Add(new InternalModelObserverInfo(
                                modelRefName,
                                methodName,
                                usedProperties,
                                isAsync,
                                hasCancellationToken));
                        }
                    }
                }
            }
            else if (LooksLikeIntendedObserverMethod(methodName))
            {
                // Method looks like it was intended to be an internal observer
                // but has invalid signature - report diagnostic
                string reason;
                if (!isPrivate)
                {
                    reason = "Method must be private to be auto-detected as internal observer.";
                }
                else
                {
                    reason = invalidReason ?? "Unknown signature issue.";
                }

                foreach (var kvp in propertiesByModelRef)
                {
                    var modelRefName = kvp.Key;
                    var accessedProperties = kvp.Value;

                    if (accessedProperties.Count > 0)
                    {
                        invalidObservers.Add(new InvalidInternalModelObserverInfo(
                            methodName,
                            modelRefName,
                            accessedProperties,
                            reason,
                            member.Identifier.GetLocation()));
                    }
                }
            }
            // Note: Methods that access model properties but don't look like intended observers
            // (e.g., command methods like AddItem, CanRefresh) are silently ignored
        }

        return (observers, invalidObservers);
    }

    /// <summary>
    /// Checks if a method name suggests it was intended to be an internal observer.
    /// Uses naming conventions like On*Changed, Handle*, *Handler, etc.
    /// </summary>
    private static bool LooksLikeIntendedObserverMethod(string methodName)
    {
        // Naming patterns that suggest the method is intended to be an observer:
        // - On*Changed, On*Updated, On*Refreshed (explicit change handlers)
        // - Handle*, *Handler (generic handlers)
        // - *Observer (explicit observer pattern)
        // - On*Async, *ChangedAsync (async versions)

        // Check prefixes
        if (methodName.StartsWith("On") && (
            methodName.EndsWith("Changed") ||
            methodName.EndsWith("ChangedAsync") ||
            methodName.EndsWith("Updated") ||
            methodName.EndsWith("UpdatedAsync") ||
            methodName.EndsWith("Refreshed") ||
            methodName.EndsWith("RefreshedAsync")))
        {
            return true;
        }

        // Handle* prefix
        if (methodName.StartsWith("Handle"))
        {
            return true;
        }

        // *Handler suffix
        if (methodName.EndsWith("Handler"))
        {
            return true;
        }

        // *Observer suffix
        if (methodName.EndsWith("Observer"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds model properties READ in a method (excludes writes/assignments).
    /// Only property reads should trigger internal observer subscriptions.
    /// </summary>
    private static Dictionary<string, List<string>> FindAccessedModelProperties(
        MethodDeclarationSyntax method,
        List<ModelReferenceInfo> modelReferences,
        Dictionary<string, (ModelReferenceInfo modelRef, string propertyName)> propertyToModelRef)
    {
        var propertiesByModelRef = new Dictionary<string, List<string>>();

        // Search for member access expressions that READ referenced model properties
        // Pattern: {ModelRefName}.{PropertyName} (e.g., Settings.AutoRefresh)
        foreach (var node in method.DescendantNodes())
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                // Skip if this should be excluded from observation:
                // - Direct assignment targets (writes)
                // - Modify-in-place reads (X = X with {...})
                if (ShouldExcludeFromObservation(memberAccess))
                {
                    continue;
                }

                // Check if the expression is a simple identifier (the model reference name)
                if (memberAccess.Expression is IdentifierNameSyntax identifier)
                {
                    var modelRefName = identifier.Identifier.ValueText;
                    var propertyName = memberAccess.Name.Identifier.ValueText;

                    // Check if this matches a model reference and property
                    var matchingModelRef = modelReferences.FirstOrDefault(mr => mr.PropertyName == modelRefName);
                    if (matchingModelRef is not null && propertyToModelRef.ContainsKey(propertyName))
                    {
                        // Verify the property belongs to this model reference
                        var (owningModelRef, _) = propertyToModelRef[propertyName];
                        if (owningModelRef.PropertyName == modelRefName)
                        {
                            if (!propertiesByModelRef.ContainsKey(modelRefName))
                            {
                                propertiesByModelRef[modelRefName] = [];
                            }

                            if (!propertiesByModelRef[modelRefName].Contains(propertyName))
                            {
                                propertiesByModelRef[modelRefName].Add(propertyName);
                            }
                        }
                    }
                }
            }
        }

        return propertiesByModelRef;
    }

    /// <summary>
    /// Checks if a member access expression should be excluded from observed properties.
    /// This includes:
    /// 1. Direct assignment targets: X = value
    /// 2. Compound assignments: X += value
    /// 3. Increment/decrement: X++, ++X
    /// 4. Modify-in-place reads: X = X with {...}, X = X + value (read is part of self-assignment)
    /// 5. Mutating method calls: X.Add(), X.Remove(), X.Clear(), etc. (collection mutations)
    /// </summary>
    private static bool ShouldExcludeFromObservation(MemberAccessExpressionSyntax memberAccess)
    {
        var parent = memberAccess.Parent;

        // Direct assignment target: Storage.Settings = "text"
        if (parent is AssignmentExpressionSyntax assignment && assignment.Left == memberAccess)
        {
            return true;
        }

        // Compound assignment: Storage.Count += 1
        if (parent is AssignmentExpressionSyntax compoundAssignment &&
            compoundAssignment.Left == memberAccess &&
            compoundAssignment.Kind() != SyntaxKind.SimpleAssignmentExpression)
        {
            return true;
        }

        // Increment/decrement: Storage.Count++
        if (parent is PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax)
        {
            return true;
        }

        // Modify-in-place pattern: X = X with {...} or X = X.Something()
        // The read of X on the right side should be excluded because it's just
        // reading the current value to create a modified copy
        if (IsModifyInPlaceRead(memberAccess))
        {
            return true;
        }

        // Mutating method calls: ErrorModel.Errors.Add(...), list.Remove(...), etc.
        // Pattern: {Model}.{Property}.{MutatingMethod}(...)
        // The access to {Model}.{Property} is a write, not a read
        if (IsMutatingMethodCall(memberAccess))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Known method names that mutate collections/objects.
    /// These are common mutating methods on ICollection, IList, ObservableList, etc.
    /// </summary>
    private static readonly HashSet<string> MutatingMethodNames =
    [
        // ICollection<T> / IList<T>
        "Add",
        "AddRange",
        "Remove",
        "RemoveAt",
        "RemoveAll",
        "RemoveRange",
        "Clear",
        "Insert",
        "InsertRange",
        // ObservableCollections specific
        "Move",
        "ReplaceRange",
        "Reset",
        // Dictionary
        "TryAdd",
        // Stack/Queue
        "Push",
        "Pop",
        "Enqueue",
        "Dequeue",
        // General setters
        "Set",
        "SetValue",
        "Update"
    ];

    /// <summary>
    /// Checks if the member access is part of a mutating method call.
    /// Pattern: {Expression}.{Property}.{MutatingMethod}(...)
    /// Example: ErrorModel.Errors.Add("message")
    /// </summary>
    private static bool IsMutatingMethodCall(MemberAccessExpressionSyntax memberAccess)
    {
        // Check if parent is another MemberAccessExpression (the method name)
        // and grandparent is InvocationExpression
        if (memberAccess.Parent is MemberAccessExpressionSyntax methodAccess &&
            methodAccess.Parent is InvocationExpressionSyntax)
        {
            var methodName = methodAccess.Name.Identifier.ValueText;
            return MutatingMethodNames.Contains(methodName);
        }

        return false;
    }

    /// <summary>
    /// Checks if a member access is a read that's part of a modify-in-place pattern.
    /// Pattern: X = X with {...} or X = X + value or X = X.Method()
    /// The read of X on the right side is just to modify it, not to observe changes.
    /// </summary>
    private static bool IsModifyInPlaceRead(MemberAccessExpressionSyntax memberAccess)
    {
        // Find the containing assignment expression
        var containingAssignment = memberAccess.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
        if (containingAssignment is null)
        {
            return false;
        }

        // Check if this member access is somewhere in the RIGHT side of the assignment
        if (!containingAssignment.Right.Contains(memberAccess))
        {
            return false;
        }

        // Get the left side - must also be a member access
        if (containingAssignment.Left is not MemberAccessExpressionSyntax leftMemberAccess)
        {
            return false;
        }

        // Compare the member access paths (e.g., "Storage.Settings" == "Storage.Settings")
        var leftPath = GetMemberAccessPath(leftMemberAccess);
        var rightPath = GetMemberAccessPath(memberAccess);

        return leftPath == rightPath;
    }

    /// <summary>
    /// Gets the full path of a member access expression (e.g., "Storage.Settings").
    /// </summary>
    private static string GetMemberAccessPath(MemberAccessExpressionSyntax memberAccess)
    {
        var parts = new List<string>();
        ExpressionSyntax? current = memberAccess;

        while (current is MemberAccessExpressionSyntax ma)
        {
            parts.Add(ma.Name.Identifier.ValueText);
            current = ma.Expression;
        }

        if (current is IdentifierNameSyntax identifier)
        {
            parts.Add(identifier.Identifier.ValueText);
        }

        parts.Reverse();
        return string.Join(".", parts);
    }

    /// <summary>
    /// Validates that the method has a valid signature for internal model observer.
    /// Valid signatures:
    /// - void MethodName() - sync
    /// - Task MethodName() - async without cancellation
    /// - Task MethodName(CancellationToken ct) - async with cancellation
    /// - ValueTask MethodName() - async without cancellation
    /// - ValueTask MethodName(CancellationToken ct) - async with cancellation
    /// </summary>
    private static (bool IsValid, bool IsAsync, bool HasCancellationToken) ValidateObserverMethodSignature(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel)
    {
        var (isValid, isAsync, hasCancellationToken, _) = ValidateObserverMethodSignatureWithReason(method, semanticModel);
        return (isValid, isAsync, hasCancellationToken);
    }

    /// <summary>
    /// Validates that the method has a valid signature for internal model observer.
    /// Returns a reason string if the signature is invalid.
    /// </summary>
    private static (bool IsValid, bool IsAsync, bool HasCancellationToken, string? InvalidReason) ValidateObserverMethodSignatureWithReason(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel)
    {
        var paramCount = method.ParameterList.Parameters.Count;

        // Get return type
        var returnTypeInfo = semanticModel.GetTypeInfo(method.ReturnType);
        var returnType = returnTypeInfo.Type;

        if (returnType is null)
        {
            return (false, false, false, "Unable to determine return type.");
        }

        // Check for void return (sync method)
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            // Sync methods must have no parameters
            if (paramCount == 0)
            {
                return (true, false, false, null);
            }

            return (false, false, false, $"Sync methods (returning void) must have no parameters. Found {paramCount} parameter(s).");
        }

        // Check for Task/ValueTask return (async method)
        var isTask = returnType.Name == "Task" &&
                     returnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";
        var isValueTask = returnType.Name == "ValueTask" &&
                          returnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";

        if (!isTask && !isValueTask)
        {
            return (false, false, false, $"Return type must be void, Task, or ValueTask. Found '{returnType.ToDisplayString()}'.");
        }

        // Async methods can have 0 or 1 parameters
        if (paramCount == 0)
        {
            return (true, true, false, null);
        }

        if (paramCount == 1)
        {
            var param = method.ParameterList.Parameters[0];
            if (param.Type is null)
            {
                return (false, false, false, "Unable to determine parameter type.");
            }

            var paramTypeInfo = semanticModel.GetTypeInfo(param.Type);
            if (paramTypeInfo.Type?.Name == "CancellationToken" &&
                paramTypeInfo.Type.ContainingNamespace?.ToDisplayString() == "System.Threading")
            {
                return (true, true, true, null);
            }

            return (false, false, false, $"Async methods can only have a CancellationToken parameter. Found parameter of type '{paramTypeInfo.Type?.ToDisplayString() ?? "unknown"}'.");
        }

        return (false, false, false, $"Async methods can have at most one parameter (CancellationToken). Found {paramCount} parameters.");
    }
}