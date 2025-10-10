using RxBlazorV2Generator.Models;
using System.Text;

namespace RxBlazorV2Generator.Generators.Templates;

/// <summary>
/// Generates dependency injection registration code for observable models.
/// </summary>
public static class DIRegistrationTemplate
{
    /// <summary>
    /// Generates the AddObservableModels extension method for non-generic models.
    /// </summary>
    /// <param name="models">Collection of non-generic model information.</param>
    /// <param name="rootNamespace">The root namespace for the extension class.</param>
    /// <returns>Generated DI registration code.</returns>
    public static string GenerateAddObservableModelsExtension(ObservableModelInfo[] models, string rootNamespace)
    {
        var sb = new StringBuilder();

        // Generate using statements
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using RxBlazorV2.Model;");

        // Add using statements for all model namespaces
        var namespaces = models.Select(m => m.Namespace).Distinct().Where(ns => !string.IsNullOrEmpty(ns)).ToArray();
        foreach (var ns in namespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        // Add using statements for interface namespaces
        var interfaceNamespaces = models
            .SelectMany(m => m.ImplementedInterfaces)
            .Select(i => ExtractNamespace(i))
            .Distinct()
            .Where(ns => !string.IsNullOrEmpty(ns) && !namespaces.Contains(ns));
        foreach (var ns in interfaceNamespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace};");
        sb.AppendLine();

        // Generate partial class with Initialize method
        sb.AppendLine("public static partial class ObservableModels");
        sb.AppendLine("{");
        sb.AppendLine("    public static IServiceCollection Initialize(IServiceCollection services)");
        sb.AppendLine("    {");

        // Collect all base model type names to exclude from DI registration
        var baseModelTypeNames = models
            .Where(m => !string.IsNullOrEmpty(m.BaseModelTypeName))
            .Select(m => m.BaseModelTypeName)
            .Distinct()
            .ToArray();

        // Generate registrations for each model (excluding base models and generic models)
        foreach (var model in models.Where(m => m.GenericTypes.Length == 0 && !baseModelTypeNames.Contains(m.FullyQualifiedName)))
        {
            sb.AppendLine(GenerateModelRegistration(model));

            // Generate interface mappings
            foreach (var interfaceType in model.ImplementedInterfaces)
            {
                var scope = GetModelScope(model);
                var registrationMethod = GetRegistrationMethod(scope);
                sb.AppendLine($"        services.{registrationMethod}<{interfaceType}>(sp => sp.GetRequiredService<{model.ClassName}>());");
            }
        }

        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates the extension methods for generic models.
    /// </summary>
    /// <param name="models">Collection of generic model information.</param>
    /// <param name="rootNamespace">The root namespace for the extension class.</param>
    /// <returns>Generated DI registration code.</returns>
    public static string GenerateAddGenericObservableModelsExtension(ObservableModelInfo[] models, string rootNamespace)
    {
        var sb = new StringBuilder();

        // Generate using statements
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using RxBlazorV2.Model;");

        // Add using statements for all model namespaces
        var namespaces = models.Select(m => m.Namespace).Distinct().Where(ns => !string.IsNullOrEmpty(ns)).ToArray();
        foreach (var ns in namespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        // Add using statements for interface namespaces
        var interfaceNamespaces = models
            .SelectMany(m => m.ImplementedInterfaces)
            .Select(i => ExtractNamespace(i))
            .Distinct()
            .Where(ns => !string.IsNullOrEmpty(ns) && !namespaces.Contains(ns));
        foreach (var ns in interfaceNamespaces)
        {
            sb.AppendLine($"using {ns};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace};");
        sb.AppendLine();

        // Generate partial class with generic registration methods
        sb.AppendLine("public static partial class ObservableModels");
        sb.AppendLine("{");

        // Collect all base model type names to exclude from DI registration
        var baseModelTypeNames = models
            .Where(m => !string.IsNullOrEmpty(m.BaseModelTypeName))
            .Select(m => m.BaseModelTypeName)
            .Distinct()
            .ToArray();

        foreach (var model in models.Where(m => m.GenericTypes.Length > 0 && !baseModelTypeNames.Contains(m.FullyQualifiedName)))
        {
            sb.AppendLine(GenerateGenericModelRegistrationMethod(model));
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates registration code for a single non-generic model.
    /// </summary>
    private static string GenerateModelRegistration(ObservableModelInfo model)
    {
        var scope = GetModelScope(model);
        var registrationMethod = GetRegistrationMethod(scope);

        // Simple registration - DI automatically resolves all constructor parameters
        // (both model references and service dependencies)
        return $"        services.{registrationMethod}<{model.ClassName}>();";
    }

    /// <summary>
    /// Generates a registration method for a generic model.
    /// </summary>
    private static string GenerateGenericModelRegistrationMethod(ObservableModelInfo model)
    {
        var sb = new StringBuilder();

        // Method signature
        sb.AppendLine($"    public static IServiceCollection {model.ClassName}{model.GenericTypes}(IServiceCollection services)");

        // Type constraints if present
        if (model.TypeConstrains.Length > 0)
        {
            sb.AppendLine($"        {model.TypeConstrains}");
        }

        sb.AppendLine("    {");

        var scope = GetModelScope(model);
        var registrationMethod = GetRegistrationMethod(scope);

        // Simple registration - DI automatically resolves all constructor parameters
        // (both model references and service dependencies)
        sb.AppendLine($"        services.{registrationMethod}<{model.ClassName}{model.GenericTypes}>();");

        // Generate interface mappings
        foreach (var interfaceType in model.ImplementedInterfaces)
        {
            sb.AppendLine($"        services.{registrationMethod}<{interfaceType}>(sp => sp.GetRequiredService<{model.ClassName}{model.GenericTypes}>());");
        }

        sb.AppendLine("        return services;");
        sb.AppendLine("    }");

        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Gets the model scope from model information.
    /// </summary>
    private static string GetModelScope(ObservableModelInfo model)
    {
        return model.ModelScope;
    }

    /// <summary>
    /// Gets the DI registration method name based on scope.
    /// </summary>
    private static string GetRegistrationMethod(string scope)
    {
        return scope switch
        {
            "Singleton" => "AddSingleton",
            "Scoped" => "AddScoped",
            "Transient" => "AddTransient",
            _ => "AddSingleton"
        };
    }

    /// <summary>
    /// Extracts the namespace from a fully qualified type name.
    /// </summary>
    private static string ExtractNamespace(string fullyQualifiedTypeName)
    {
        var lastDotIndex = fullyQualifiedTypeName.LastIndexOf('.');
        return lastDotIndex > 0 ? fullyQualifiedTypeName.Substring(0, lastDotIndex) : "";
    }
}
