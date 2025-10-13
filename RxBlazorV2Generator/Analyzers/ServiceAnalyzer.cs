using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;

namespace RxBlazorV2Generator.Analyzers;

public static class ServiceAnalyzer
{
    public static bool IsServiceRegistration(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        return invocation.IsServiceRegistration();
    }

    public static (ServiceInfoList? serviceInfoList, List<Diagnostic> diagnostics) GetServiceClassInfoWithDiagnostics(
        GeneratorSyntaxContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var serviceInfoList = new ServiceInfoList();

        try
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
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
                        if (typeSymbol != null)
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
                                if (typeSymbol != null)
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
                                if (typeSymbol != null)
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
        }
        catch (Exception ex)
        {
            // Report diagnostic for the invocation expression
            var invocation = (InvocationExpressionSyntax)context.Node;
            var location = invocation.GetLocation();
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                location,
                "ServiceAnalysis",
                ex.Message);
            diagnostics.Add(diagnostic);
            return (null, diagnostics);
        }
        
        return (serviceInfoList, diagnostics);
    }

    public static ServiceInfoList? GetServiceClassInfo(GeneratorSyntaxContext context)
    {
        var (serviceInfo, _) = GetServiceClassInfoWithDiagnostics(context);
        return serviceInfo;
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