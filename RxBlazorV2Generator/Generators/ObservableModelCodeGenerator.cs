using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Generators.Templates;
using System.Text;

namespace RxBlazorV2Generator.Generators;

public static class ObservableModelCodeGenerator
{
    public static void GenerateObservableModelPartials(SourceProductionContext context, ObservableModelInfo modelInfo)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("#nullable enable");

            // Add all using statements from the source file
            var requiredUsings = new HashSet<string>(modelInfo.UsingStatements);

            // Add required framework usings
            requiredUsings.Add("JetBrains.Annotations");
            requiredUsings.Add("Microsoft.Extensions.DependencyInjection");
            requiredUsings.Add("ObservableCollections");
            requiredUsings.Add("R3");
            requiredUsings.Add("RxBlazorV2.Interface");
            requiredUsings.Add("RxBlazorV2.Model");
            requiredUsings.Add("System");
            
            // Add System.Linq if there are model references
            if (modelInfo.ModelReferences.Any())
            {
                requiredUsings.Add("System.Linq");
            }

            // Add using statements for referenced model namespaces
            if (modelInfo.ModelReferences.Any())
            {
                foreach (var modelRef in modelInfo.ModelReferences)
                {
                    if (!string.IsNullOrEmpty(modelRef.ReferencedModelNamespace) &&
                        modelRef.ReferencedModelNamespace != modelInfo.Namespace)
                    {
                        requiredUsings.Add(modelRef.ReferencedModelNamespace);
                    }
                }
            }

            // Sort and add all using statements
            foreach (var usingStatement in requiredUsings.OrderBy(u => u))
            {
                sb.AppendLine($"using {usingStatement};");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {modelInfo.Namespace};");
            sb.AppendLine();
            sb.AppendLine($"{modelInfo.ClassAccessibility} partial class {modelInfo.ClassName}{modelInfo.GenericTypes}");
            sb.AppendLine("{");

            // Generate ModelID property implementation
            sb.AppendLine(PropertyTemplate.GenerateModelIDProperty(modelInfo.FullyQualifiedName));
            sb.AppendLine();

            // Generate FilterUsedProperties method implementation
            sb.AppendLine(PropertyTemplate.GenerateFilterUsedPropertiesMethod(modelInfo.ModelReferences));
           
            // Generate protected properties for referenced models
            if (modelInfo.ModelReferences.Any())
            {
                sb.AppendLine(PropertyTemplate.GenerateModelReferenceProperties(modelInfo.ModelReferences));
                sb.AppendLine();
            }

            // Generate protected properties for DI injected services
            if (modelInfo.DIFields.Any())
            {
                sb.AppendLine(PropertyTemplate.GenerateDIFieldProperties(modelInfo.DIFields));
                sb.AppendLine();
            }

            // Generate partial property implementations with field keyword
            // Uses fully qualified property names (ClassName.PropertyName) for disambiguation
            if (modelInfo.PartialProperties.Any())
            {
                var classNameWithGenerics = modelInfo.ClassName + modelInfo.GenericTypes;
                sb.Append(PropertyTemplate.GeneratePartialProperties(modelInfo.PartialProperties, classNameWithGenerics));
            }

            // Generate backing fields for command properties
            if (modelInfo.CommandProperties.Any())
            {
                sb.AppendLine();
                sb.Append(CommandTemplate.GenerateBackingFields(modelInfo.CommandProperties));
                sb.AppendLine();
            }

            // Generate command property implementations with backing fields
            if (modelInfo.CommandProperties.Any())
            {
                sb.AppendLine();
                sb.Append(CommandTemplate.GenerateCommandProperties(modelInfo.CommandProperties));
            }

            // Generate constructor
            var constructorCode = ConstructorTemplate.GenerateConstructor(modelInfo, ObservableModelAnalyzer.GetObservedProperties);
            if (!string.IsNullOrEmpty(constructorCode))
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(constructorCode);
            }

            // Generate OnContextReadyIntern() for referenced models and model observers in services
            var onContextReadyCode = ModelObserverTemplate.GenerateOnContextReadyIntern(
                modelInfo.ModelReferences,
                modelInfo.ModelObservers);
            if (!string.IsNullOrEmpty(onContextReadyCode))
            {
                sb.Append(onContextReadyCode);
            }

            // Generate OnContextReadyInternAsync() for referenced models
            var onContextReadyAsyncCode = ModelObserverTemplate.GenerateOnContextReadyInternAsync(modelInfo.ModelReferences);
            if (!string.IsNullOrEmpty(onContextReadyAsyncCode))
            {
                sb.Append(onContextReadyAsyncCode);
            }

            sb.AppendLine();
            sb.AppendLine("}");


            // Remove top namespace and use dots where possible
            var namespaceParts = modelInfo.Namespace.Split('.');
            var relativeNamespace =
                namespaceParts.Length > 1 ? string.Join(".", namespaceParts.Skip(1)) : namespaceParts[0];
            var fileName = $"{relativeNamespace}.{modelInfo.ClassName}.g.cs";
            context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                modelInfo.ClassName,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    public static void GenerateAddObservableModelsExtension(SourceProductionContext context,
        ObservableModelInfo[] models, string rootNamespace)
    {
        if (models.Length == 0)
        {
            return;
        }

        try
        {
            var code = DIRegistrationTemplate.GenerateAddObservableModelsExtension(models, rootNamespace);
            context.AddSource("ObservableModelsServiceCollectionExtension.g.cs", SourceText.From(code, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                "ObservableModelsServices",
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    public static void GenerateAddGenericObservableModelsExtension(SourceProductionContext context,
        ObservableModelInfo[] models, string rootNamespace)
    {
        if (models.Length == 0)
        {
            return;
        }

        try
        {
            var code = DIRegistrationTemplate.GenerateAddGenericObservableModelsExtension(models, rootNamespace);
            context.AddSource("GenericModelsServiceCollectionExtension.g.cs", SourceText.From(code, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                "GenericModelsServices",
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }
}