using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;

namespace RxBlazorV2Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RxBlazorDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor[] AllDiagnostics =
    [
        DiagnosticDescriptors.ObservableModelAnalysisError,
        DiagnosticDescriptors.RazorAnalysisError,
        DiagnosticDescriptors.CodeGenerationError,
        DiagnosticDescriptors.MethodAnalysisWarning,
        DiagnosticDescriptors.RazorFileReadError,
        DiagnosticDescriptors.CircularModelReferenceError,
        DiagnosticDescriptors.InvalidModelReferenceTargetError,
        DiagnosticDescriptors.UnusedModelReferenceError,
        DiagnosticDescriptors.ComponentNotObservableWarning,
        DiagnosticDescriptors.SharedModelNotSingletonError,
        DiagnosticDescriptors.TriggerTypeArgumentsMismatchError,
        DiagnosticDescriptors.CircularTriggerReferenceError,
        DiagnosticDescriptors.GenericArityMismatchError,
        DiagnosticDescriptors.TypeConstraintMismatchError,
        DiagnosticDescriptors.InvalidOpenGenericReferenceError,
        DiagnosticDescriptors.InvalidInitPropertyError,
        DiagnosticDescriptors.DerivedModelReferenceError,
        DiagnosticDescriptors.RazorInheritanceMismatchWarning,
        DiagnosticDescriptors.UnregisteredServiceWarning,
        DiagnosticDescriptors.DiServiceScopeViolationWarning
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(AllDiagnostics);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeObservableModelClass, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeRazorCodeBehindClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeObservableModelClass(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        
        // Only analyze classes that might be ObservableModel classes
        if (!ObservableModelAnalyzer.IsObservableModelClass(classDecl)) 
            return;

        try
        {
            // Use the analyzer-compatible method to get diagnostics
            var diagnostics = AnalyzeObservableModelForDiagnostics(classDecl, context.SemanticModel, context.Compilation);
            
            // Report all diagnostics found
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
        catch (Exception ex)
        {
            // Report analysis error if something goes wrong
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                classDecl.Identifier.GetLocation(),
                classDecl.Identifier.ValueText,
                ex.Message);
            
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeRazorCodeBehindClass(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        // Only analyze classes that might be Razor code-behind classes
        if (!RazorAnalyzer.IsRazorCodeBehindClass(classDecl, context.SemanticModel))
            return;

        try
        {
            // Use the analyzer-compatible method to get diagnostics
            var diagnostics = AnalyzeRazorCodeBehindForDiagnostics(classDecl, context.SemanticModel, context.Compilation);

            // Report all diagnostics found
            foreach (var diagnostic in diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
        catch (Exception ex)
        {
            // Report analysis error if something goes wrong
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.RazorAnalysisError,
                classDecl.Identifier.GetLocation(),
                classDecl.Identifier.ValueText,
                ex.Message);

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Analyzer-compatible method to get diagnostics from ObservableModel classes using existing generator logic
    /// </summary>
    private static List<Diagnostic> AnalyzeObservableModelForDiagnostics(ClassDeclarationSyntax classDecl, SemanticModel semanticModel, Compilation compilation)
    {
        var diagnostics = new List<Diagnostic>();

        try
        {
            if (semanticModel.GetDeclaredSymbol(classDecl) is not { } namedTypeSymbol)
                return diagnostics;

            // Check if class inherits from ObservableModel
            if (!namedTypeSymbol.InheritsFromObservableModel())
                return diagnostics;

            // Collect service registrations from the compilation
            var serviceClasses = CollectServiceRegistrations(compilation);

            // Extract model references and DI fields from partial constructor parameters
            var (modelReferences, diFields, unregisteredServices, diFieldsWithScope) = classDecl.ExtractPartialConstructorDependencies(semanticModel, serviceClasses);

            // Report informational warnings for unregistered services
            foreach (var (paramName, paramType, typeSymbol, location) in unregisteredServices)
            {
                if (location is not null && typeSymbol is not null)
                {
                    // Get simple type name (without namespace)
                    var simpleTypeName = typeSymbol.Name;

                    // Determine if it's an interface
                    var isInterface = typeSymbol.TypeKind == TypeKind.Interface;

                    // Create appropriate registration example
                    var registrationExample = isInterface
                        ? $"'services.AddScoped<{simpleTypeName}, YourImplementation>()'"
                        : $"'services.AddScoped<{simpleTypeName}>()' or 'services.AddScoped<IYourInterface, {simpleTypeName}>()'";

                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.UnregisteredServiceWarning,
                        location,
                        paramName,
                        simpleTypeName,
                        registrationExample);
                    diagnostics.Add(diagnostic);
                }
            }

            // Check for DI scope violations
            var modelScope = classDecl.ExtractModelScopeFromClass(semanticModel);
            foreach (var (diField, serviceScope, location) in diFieldsWithScope)
            {
                if (serviceScope != null && location != null)
                {
                    var violation = CheckScopeViolation(modelScope, serviceScope);
                    if (violation != null)
                    {
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.DiServiceScopeViolationWarning,
                            location,
                            namedTypeSymbol.Name,
                            modelScope,
                            diField.FieldName,
                            diField.FieldType,
                            serviceScope);
                        diagnostics.Add(diagnostic);
                    }
                }
            }

            var methods = classDecl.CollectMethods();
            var (commandProperties, commandPropertiesDiagnostics) = classDecl.ExtractCommandPropertiesWithDiagnostics(methods, semanticModel);
            diagnostics.AddRange(commandPropertiesDiagnostics);

            var (partialProperties, partialPropertyDiagnostics) = classDecl.ExtractPartialPropertiesWithDiagnostics(semanticModel);
            diagnostics.AddRange(partialPropertyDiagnostics);

            // Check for unused model references (RXBG008)
            // Build symbol map for model references
            var modelSymbolMap = new Dictionary<string, ITypeSymbol>();
            foreach (var modelRef in modelReferences)
            {
                var fullTypeName = string.IsNullOrEmpty(modelRef.ReferencedModelNamespace)
                    ? modelRef.ReferencedModelTypeName
                    : $"{modelRef.ReferencedModelNamespace}.{modelRef.ReferencedModelTypeName}";

                var typeSymbol = compilation.GetTypeByMetadataName(fullTypeName);
                if (typeSymbol != null)
                {
                    modelSymbolMap[modelRef.PropertyName] = typeSymbol;
                }
            }

            // Create temporary model info for enhancement analysis
            var tempModelInfo = new Models.ObservableModelInfo(
                namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                namedTypeSymbol.Name,
                namedTypeSymbol.ToDisplayString(),
                new List<Models.PartialPropertyInfo>(),
                commandProperties,
                methods,
                modelReferences,
                "Singleton",
                new List<Models.DIFieldInfo>(),
                new List<string>(),
                null,
                null,
                new List<string>());

            // Enhance model references with command method analysis
            var enhancedModelReferences = tempModelInfo.EnhanceModelReferencesWithCommandAnalysis(
                semanticModel,
                modelSymbolMap);

            // Check for unused model references using SSOT helper
            var unusedDiagnostics = enhancedModelReferences.CreateUnusedModelReferenceDiagnostics(
                namedTypeSymbol,
                classDecl);
            diagnostics.AddRange(unusedDiagnostics);

            // Check for shared model scoping issues using compilation
            var sharedModelDiagnostics = AnalyzeSharedModelScoping(classDecl, semanticModel, compilation);
            diagnostics.AddRange(sharedModelDiagnostics);
        }
        catch (Exception ex)
        {
            // Report diagnostic instead of throwing
            var location = classDecl.Identifier.GetLocation();
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                location,
                classDecl.Identifier.ValueText,
                ex.Message);
            diagnostics.Add(diagnostic);
        }
        
        return diagnostics;
    }

    /// <summary>
    /// Analyzes shared model scoping using compilation information available in syntax node context
    /// </summary>
    private static List<Diagnostic> AnalyzeSharedModelScoping(ClassDeclarationSyntax classDecl, SemanticModel semanticModel, Compilation compilation)
    {
        var diagnostics = new List<Diagnostic>();
        
        try
        {
            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol namedTypeSymbol)
                return diagnostics;

            // Get the model scope from attributes
            var modelScope = "Singleton"; // Default scope
            Location? attributeLocation = null;

            foreach (var attribute in namedTypeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "ObservableModelScopeAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0 && 
                        attribute.ConstructorArguments[0].Value is int scopeValue)
                    {
                        modelScope = scopeValue switch
                        {
                            0 => "Singleton",
                            1 => "Scoped",
                            2 => "Transient",
                            _ => "Singleton"
                        };
                    }
                    attributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    break;
                }
            }

            // Only check non-singleton models
            if (modelScope != "Singleton")
            {
                // Count how many components use this model by searching the compilation
                var modelUsageCount = CountModelUsageInCompilation(namedTypeSymbol, compilation);
                
                if (modelUsageCount > 1)
                {
                    var location = attributeLocation ?? classDecl.GetLocation();
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.SharedModelNotSingletonError,
                        location,
                        namedTypeSymbol.ToDisplayString(),
                        modelScope);
                    diagnostics.Add(diagnostic);
                }
            }
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                classDecl.GetLocation(),
                classDecl.Identifier.ValueText,
                ex.Message);
            diagnostics.Add(diagnostic);
        }
        
        return diagnostics;
    }

    /// <summary>
    /// Counts how many components in the compilation use this model type.
    /// Note: This approach iterates through compilation symbols which is more efficient
    /// than syntax tree traversal for cross-file analysis in syntax node context.
    /// </summary>
    private static int CountModelUsageInCompilation(INamedTypeSymbol modelType, Compilation compilation)
    {
        var usageCount = 0;
        
        // Get all types in the compilation that inherit from ObservableComponent<T>
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(type => GetObservableComponentBaseModelType(type) != null);
        
        foreach (var componentType in allTypes)
        {
            var baseModelType = GetObservableComponentBaseModelType(componentType);
            if (baseModelType != null && SymbolEqualityComparer.Default.Equals(baseModelType, modelType))
            {
                usageCount++;
            }
        }
        
        return usageCount;
    }

    /// <summary>
    /// Gets the model type from ObservableComponent&lt;T&gt; base class
    /// </summary>
    private static INamedTypeSymbol? GetObservableComponentBaseModelType(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "ObservableComponent" && baseType.TypeArguments.Length == 1)
            {
                return baseType.TypeArguments[0] as INamedTypeSymbol;
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    /// <summary>
    /// Checks if there's a DI scope violation between model scope and service scope.
    /// Returns a message describing the violation, or null if no violation.
    ///
    /// DI Scoping Rules:
    /// - Singleton can only inject Singleton
    /// - Scoped can inject Singleton or Scoped
    /// - Transient can inject anything
    /// </summary>
    private static string? CheckScopeViolation(string modelScope, string serviceScope)
    {
        if (modelScope == "Singleton" && serviceScope != "Singleton")
        {
            return $"Singleton services cannot depend on {serviceScope} services (captive dependency)";
        }

        if (modelScope == "Scoped" && serviceScope == "Transient")
        {
            return $"Scoped services should not depend on Transient services (may cause issues with disposal)";
        }

        return null;
    }

    /// <summary>
    /// Analyzer-compatible method to get diagnostics from Razor code-behind classes.
    /// Uses the same detection logic as the generator (SSOT) by calling RazorAnalyzer.GetRazorCodeBehindInfo.
    ///
    /// Note: RXBG019 (RazorInheritanceMismatchWarning) is reported by the source generator instead of the analyzer
    /// because it requires access to AdditionalTexts (.razor files) which analyzers cannot efficiently access.
    /// </summary>
    private static List<Diagnostic> AnalyzeRazorCodeBehindForDiagnostics(ClassDeclarationSyntax classDecl, SemanticModel semanticModel, Compilation compilation)
    {
        var diagnostics = new List<Diagnostic>();

        // Use the same detection logic as the generator
        var razorInfo = RazorAnalyzer.GetRazorCodeBehindInfo(classDecl, semanticModel);

        // Check if we found a diagnostic issue
        if (razorInfo?.HasDiagnosticIssue == true)
        {
            var location = classDecl.Identifier.GetLocation();
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ComponentNotObservableWarning,
                location,
                classDecl.Identifier.Text);

            diagnostics.Add(diagnostic);
        }

        return diagnostics;
    }

    /// <summary>
    /// Collects DI service registrations from the semantic model's syntax tree.
    /// Scans for AddSingleton/AddScoped/AddTransient calls.
    /// </summary>
    private static ServiceInfoList? CollectServiceRegistrations(Compilation compilation)
    {
        var mergedServices = new ServiceInfoList();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            // Find all invocation expressions that look like service registrations
            var invocations = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation => ServiceAnalyzer.IsServiceRegistration(invocation));

            foreach (var invocation in invocations)
            {
                // Analyze the service registration directly
                var serviceInfo = AnalyzeServiceRegistration(invocation, semanticModel);

                if (serviceInfo is not null)
                {
                    foreach (var service in serviceInfo.Services)
                    {
                        mergedServices.AddService(service);
                    }
                }
            }
        }

        return mergedServices.Services.Any() ? mergedServices : null;
    }

    /// <summary>
    /// Analyzes a service registration invocation and extracts service info.
    /// This is a simplified version of ServiceAnalyzer.GetServiceClassInfo that works in analyzer context.
    /// </summary>
    private static ServiceInfoList? AnalyzeServiceRegistration(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var serviceInfoList = new ServiceInfoList();

        try
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return null;
            }

            // Extract scope from method name (AddSingleton, AddScoped, AddTransient)
            var methodName = memberAccess.Name is GenericNameSyntax genericName
                ? genericName.Identifier.ValueText
                : memberAccess.Name.Identifier.ValueText;

            var scope = ExtractScopeFromMethodName(methodName);

            // Handle generic service registrations like AddScoped<MyService>()
            if (memberAccess.Name is GenericNameSyntax genericNameSyntax)
            {
                foreach (var typeArg in genericNameSyntax.TypeArgumentList.Arguments)
                {
                    var typeSymbol = semanticModel.GetTypeInfo(typeArg).Type as INamedTypeSymbol;
                    if (typeSymbol is not null)
                    {
                        serviceInfoList.AddService(new ServiceInfo(
                            typeSymbol.ContainingNamespace.ToDisplayString(),
                            typeSymbol.Name,
                            typeSymbol.ToDisplayString(),
                            scope));
                    }
                }
            }
            // Handle factory registrations like AddScoped(sp => new HttpClient())
            else if (invocation.ArgumentList.Arguments.Count > 0)
            {
                foreach (var argument in invocation.ArgumentList.Arguments)
                {
                    // Look for lambda expressions or delegates that create instances
                    if (argument.Expression is SimpleLambdaExpressionSyntax lambda)
                    {
                        // Find object creation expressions in the lambda body
                        var objectCreations = lambda.Body.DescendantNodesAndSelf()
                            .OfType<ObjectCreationExpressionSyntax>();

                        foreach (var objectCreation in objectCreations)
                        {
                            var typeSymbol = semanticModel.GetTypeInfo(objectCreation.Type).Type as INamedTypeSymbol;
                            if (typeSymbol is not null)
                            {
                                serviceInfoList.AddService(new ServiceInfo(
                                    typeSymbol.ContainingNamespace.ToDisplayString(),
                                    typeSymbol.Name,
                                    typeSymbol.ToDisplayString(),
                                    scope));
                            }
                        }
                    }
                    // Also check for parenthesized lambda expressions
                    else if (argument.Expression is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                    {
                        var objectCreations = parenthesizedLambda.Body.DescendantNodesAndSelf()
                            .OfType<ObjectCreationExpressionSyntax>();

                        foreach (var objectCreation in objectCreations)
                        {
                            var typeSymbol = semanticModel.GetTypeInfo(objectCreation.Type).Type as INamedTypeSymbol;
                            if (typeSymbol is not null)
                            {
                                serviceInfoList.AddService(new ServiceInfo(
                                    typeSymbol.ContainingNamespace.ToDisplayString(),
                                    typeSymbol.Name,
                                    typeSymbol.ToDisplayString(),
                                    scope));
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            return null;
        }

        return serviceInfoList.Services.Any() ? serviceInfoList : null;
    }

    /// <summary>
    /// Extracts the service scope (Singleton, Scoped, Transient) from the DI registration method name.
    /// </summary>
    private static string? ExtractScopeFromMethodName(string methodName)
    {
        if (methodName.IndexOf("Singleton", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Singleton";
        }

        if (methodName.IndexOf("Scoped", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Scoped";
        }

        if (methodName.IndexOf("Transient", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "Transient";
        }

        return null;
    }
}