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
                    var validatedSymbol = ValidateAndReturnTypeSymbol(namedTypeSymbol, attribute, semanticModel, serviceInfoList);
                    return (validatedSymbol, null);
                }
            }
            // Try to get the type from non-generic attribute with typeof() argument
            else if (attribute.ArgumentList?.Arguments.Count > 0)
            {
                var firstArgument = attribute.ArgumentList.Arguments.First();

                // Handle typeof(SomeType) expressions
                if (firstArgument.Expression is TypeOfExpressionSyntax typeOfExpression)
                {
                    var typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);

                    if (typeInfo.Type is INamedTypeSymbol namedTypeSymbol)
                    {
                        // For unbound generic types, we might get the constructed generic type
                        // Try to get the generic type definition if this is a constructed generic type
                        var typeToValidate = namedTypeSymbol.IsGenericType && namedTypeSymbol.TypeArguments.Length > 0
                            ? namedTypeSymbol.ConstructedFrom
                            : namedTypeSymbol;

                        var validatedSymbol = ValidateAndReturnTypeSymbol(typeToValidate, attribute, semanticModel, serviceInfoList);
                        return (validatedSymbol, null);
                    }
                }
            }
        }
        catch (Exceptions.DiagnosticException ex)
        {
            // Convert DiagnosticException to diagnostic
            return (null, ex.ToDiagnostic());
        }
        catch (Exception)
        {
            // Return null on any other error during extraction
        }
        return (null, null);
    }

    private static INamedTypeSymbol ValidateAndReturnTypeSymbol(
        INamedTypeSymbol namedTypeSymbol,
        AttributeSyntax attribute,
        SemanticModel semanticModel,
        ServiceInfoList? serviceInfoList)
    {
        // Check if type inherits from ObservableModel or IObservableModel
        if (!namedTypeSymbol.InheritsFromObservableModel() && !namedTypeSymbol.InheritsFromIObservableModel())
        {
            throw new Exceptions.DiagnosticException(
                DiagnosticDescriptors.InvalidModelReferenceTargetError,
                attribute.GetLocation(),
                namedTypeSymbol.Name);
        }

        // Check for ambiguous model references (RXBG008)
        // Find all types in the compilation with the same simple name
        var typeName = namedTypeSymbol.Name;
        var matchingTypes = new List<INamedTypeSymbol>();

        // Search through source compilation global namespace
        FindTypesWithName(semanticModel.Compilation.GlobalNamespace, typeName, matchingTypes);

        // Also search through referenced assemblies
        foreach (var reference in semanticModel.Compilation.References)
        {
            if (semanticModel.Compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
            {
                FindTypesWithName(assemblySymbol.GlobalNamespace, typeName, matchingTypes);
            }
        }

        // Filter to only ObservableModel types and exclude the one we already resolved
        var observableModelMatches = matchingTypes
            .Where(t => (t.InheritsFromObservableModel() || t.InheritsFromIObservableModel()) &&
                       !SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, namedTypeSymbol.OriginalDefinition))
            .ToList();

        if (observableModelMatches.Any())
        {
            // Found multiple ObservableModel types with the same name
            var namespaces = new[] { namedTypeSymbol.ContainingNamespace.ToDisplayString() }
                .Concat(observableModelMatches.Select(t => t.ContainingNamespace.ToDisplayString()))
                .Distinct()
                .OrderBy(ns => ns);

            throw new Exceptions.DiagnosticException(
                DiagnosticDescriptors.AmbiguousModelReferenceError,
                attribute.GetLocation(),
                typeName,
                string.Join(", ", namespaces));
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
        }

        return namedTypeSymbol;
    }

    private static void FindTypesWithName(INamespaceSymbol namespaceSymbol, string typeName, List<INamedTypeSymbol> results)
    {
        // Search for types in this namespace
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamedTypeSymbol namedType && namedType.Name == typeName)
            {
                results.Add(namedType);
            }
            else if (member is INamespaceSymbol childNamespace)
            {
                // Recursively search child namespaces
                FindTypesWithName(childNamespace, typeName, results);
            }
        }
    }

    public static INamedTypeSymbol ValidateGenericTypeConstraints(
        this INamedTypeSymbol referencedType,
        INamedTypeSymbol referencingType,
        AttributeSyntax attribute)
    {
        // If the referenced type is not generic, no constraint validation needed
        if (!referencedType.IsGenericType)
        {
            return referencedType;
        }

        // Check if referenced type is an open generic type
        // For unbound generics like typeof(MyClass<,>), we need to check if it has type parameters
        var isOpenGeneric = referencedType.IsGenericType &&
                           (referencedType.IsUnboundGenericType ||
                            referencedType.TypeParameters.Length > 0);

        if (isOpenGeneric)
        {
            // Referencing type must also be generic
            if (!referencingType.IsGenericType)
            {
                throw new Exceptions.DiagnosticException(
                    DiagnosticDescriptors.InvalidOpenGenericReferenceError,
                    attribute.GetLocation(),
                    referencedType.Name,
                    referencingType.Name);
            }

            // Check that generic arity matches
            var referencedArity = referencedType.TypeParameters.Length;
            var referencingArity = referencingType.TypeParameters.Length;

            if (referencedArity != referencingArity)
            {
                throw new Exceptions.DiagnosticException(
                    DiagnosticDescriptors.GenericArityMismatchError,
                    attribute.GetLocation(),
                    referencedType.Name,
                    referencedArity,
                    referencingType.Name,
                    referencingArity);
            }

            // Validate type parameter constraints
            for (int i = 0; i < referencedArity; i++)
            {
                var referencedParam = referencedType.TypeParameters[i];
                var referencingParam = referencingType.TypeParameters[i];

                ValidateTypeParameterConstraints(referencedParam, referencingParam, referencedType, referencingType, attribute);
            }

            // Return the unbound generic type for code generation
            return referencedType;
        }

        // For closed generic types, just return as-is
        return referencedType;
    }

    private static void ValidateTypeParameterConstraints(
        ITypeParameterSymbol referencedParam,
        ITypeParameterSymbol referencingParam,
        INamedTypeSymbol referencedType,
        INamedTypeSymbol referencingType,
        AttributeSyntax attribute)
    {
        // Compare constraint types
        var referencedConstraints = GetConstraintString(referencedParam);
        var referencingConstraints = GetConstraintString(referencingParam);

        // Simple constraint comparison - could be enhanced for more sophisticated validation
        if (referencedConstraints != referencingConstraints)
        {
            throw new Exceptions.DiagnosticException(
                DiagnosticDescriptors.TypeConstraintMismatchError,
                attribute.GetLocation(),
                referencedParam.Name,
                referencedType.Name,
                referencedConstraints,
                referencingType.Name,
                referencingConstraints);
        }
    }

    private static string GetConstraintString(ITypeParameterSymbol typeParam)
    {
        var constraints = new List<string>();

        if (typeParam.HasReferenceTypeConstraint)
            constraints.Add("class");
        
        if (typeParam.HasValueTypeConstraint)
            constraints.Add("struct");
            
        if (typeParam.HasUnmanagedTypeConstraint)
            constraints.Add("unmanaged");
            
        if (typeParam.HasNotNullConstraint)
            constraints.Add("notnull");

        foreach (var constraintType in typeParam.ConstraintTypes)
        {
            constraints.Add(constraintType.ToDisplayString());
        }

        if (typeParam.HasConstructorConstraint)
            constraints.Add("new()");

        return constraints.Any() ? string.Join(", ", constraints) : "none";
    }

    private static Diagnostic? DetectCircularReference(
        INamedTypeSymbol referencingType,
        INamedTypeSymbol referencedType,
        AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        // Get the original (unbound) type for comparison
        var referencingOriginal = referencingType.OriginalDefinition;
        var referencedOriginal = referencedType.OriginalDefinition;

        // Check if the referenced type has an ObservableModelReference attribute back to the referencing type
        var referencedDeclaration = referencedOriginal.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ClassDeclarationSyntax;
        if (referencedDeclaration is null)
        {
            return null;
        }

        foreach (var attributeList in referencedDeclaration.AttributeLists)
        {
            foreach (var attr in attributeList.Attributes)
            {
                if (attr.IsObservableModelReference(semanticModel))
                {
                    // Extract the type referenced by the other model
                    var (otherReferencedType, _) = attr.ExtractReferencedModelTypeWithDiagnostic(semanticModel);
                    if (otherReferencedType is not null)
                    {
                        var otherReferencedOriginal = otherReferencedType.OriginalDefinition;

                        // Check if it references back to the original referencing type
                        if (SymbolEqualityComparer.Default.Equals(otherReferencedOriginal, referencingOriginal))
                        {
                            var diagnostic = Diagnostic.Create(
                                DiagnosticDescriptors.CircularModelReferenceError,
                                attribute.GetLocation(),
                                referencingType.Name,
                                referencedType.Name);
                            return diagnostic;
                        }
                    }
                }
            }
        }

        return null;
    }

    private static (string propertyTypeName, string propertyName) GetPropertyTypeAndName(
        INamedTypeSymbol referencedType,
        INamedTypeSymbol referencingType)
    {
        var propertyName = referencedType.Name;
        
        // If the referenced type is an open generic type, construct the concrete type  
        var isOpenGeneric = referencedType.IsGenericType && 
                           (referencedType.IsUnboundGenericType || 
                            referencedType.TypeParameters.Length > 0);
                            
        if (isOpenGeneric && referencingType.IsGenericType)
        {
            // For open generic types, use the type parameters from the referencing type
            var typeParameters = referencingType.TypeParameters.Select(tp => tp.Name).ToArray();
            var concreteTypeName = $"{referencedType.Name}<{string.Join(", ", typeParameters)}>";
            
            return (concreteTypeName, propertyName);
        }
        
        // For closed generic types or non-generic types, use the full type name
        var fullTypeName = referencedType.ToDisplayString();
        return (fullTypeName, propertyName);
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
                a.IsObservableCommand(semanticModel));

            if (commandAttr != null)
            {
                var executeMethodArg = commandAttr.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString();
                var canExecuteMethodArg = commandAttr.ArgumentList?.Arguments.Skip(1).FirstOrDefault()?.Expression.ToString();

                // Remove nameof() wrapper and quotes
                var executeMethod = executeMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');
                var canExecuteMethod = canExecuteMethodArg?.Replace("nameof(", "").Replace(")", "").Trim('"');

                // Analyze the execute method to determine if it supports cancellation
                var supportsCancellation = methods.TryGetValue(executeMethod!, out var executeMethodSyntax) &&
                    semanticModel != null &&
                    executeMethodSyntax.HasCancellationTokenParameter(semanticModel);

                // Extract trigger attributes
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
                if (attribute.IsObservableModelReference(semanticModel))
                {
                    // Extract the generic type parameter with diagnostic information
                    var (referencedModelType, diagnostic) = attribute.ExtractReferencedModelTypeWithDiagnostic(semanticModel, serviceInfoList);
                    
                    if (diagnostic != null)
                    {
                        diagnostics.Add(diagnostic);
                    }
                    
                    if (referencedModelType != null)
                    {
                        // Get the referencing class symbol for constraint validation
                        var referencingClassSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                        if (referencingClassSymbol is INamedTypeSymbol referencingTypeSymbol)
                        {
                            // Check for circular reference
                            var circularDiagnostic = DetectCircularReference(referencingTypeSymbol, referencedModelType, attribute, semanticModel);
                            if (circularDiagnostic != null)
                            {
                                diagnostics.Add(circularDiagnostic);
                                continue; // Skip adding this reference if circular reference detected
                            }

                            try
                            {
                                // Validate generic type constraints if the referenced type is generic
                                var validatedType = referencedModelType.ValidateGenericTypeConstraints(referencingTypeSymbol, attribute);

                                // For open generic types, we need to construct the concrete type name using referencing class's type parameters
                                var (propertyTypeName, propertyName) = GetPropertyTypeAndName(validatedType, referencingTypeSymbol);
                                var usedProperties = classDecl.AnalyzeModelReferenceUsage(
                                    propertyName,
                                    semanticModel,
                                    validatedType);

                                modelReferences.Add(new ModelReferenceInfo(
                                    propertyTypeName,
                                    validatedType.ContainingNamespace.ToDisplayString(),
                                    propertyName,
                                    usedProperties));
                            }
                            catch (Exceptions.DiagnosticException ex)
                            {
                                diagnostics.Add(ex.ToDiagnostic());
                                continue; // Skip adding this reference if validation failed
                            }
                        }
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
}