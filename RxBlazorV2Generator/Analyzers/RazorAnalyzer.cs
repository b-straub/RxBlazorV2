using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;

namespace RxBlazorV2Generator.Analyzers;

public static class RazorAnalyzer
{
    private const string OwningComponentBase = "Microsoft.AspNetCore.Components.OwningComponentBase";
    public static bool IsRazorCodeBehindClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl &&
               classDecl.BaseList?.Types.Any(t =>
               {
                   var typeName = t.Type.ToString();
                   return typeName.Contains("ObservableComponent") ||
                          typeName.Contains("ComponentBase") ||
                          typeName.Contains("OwningComponentBase") ||
                          typeName.EndsWith(".ComponentBase") ||
                          typeName.EndsWith(".OwningComponentBase");
               }) == true;
    }

    public static bool IsRazorCodeBehindClass(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        if (classDecl.BaseList?.Types.Count > 0)
        {
            foreach (var baseType in classDecl.BaseList.Types)
            {
                var typeInfo = semanticModel.GetTypeInfo(baseType.Type);
                if (typeInfo.Type is INamedTypeSymbol baseTypeSymbol)
                {
                    // Check if this type or any of its base types are component base types
                    var currentType = baseTypeSymbol;
                    while (currentType != null)
                    {
                        var fullName = currentType.ToDisplayString();
                        if (fullName.Contains("ComponentBase") ||
                            fullName.Contains("OwningComponentBase") ||
                            fullName.Contains("ObservableComponent"))
                        {
                            return true;
                        }
                        currentType = currentType.BaseType;
                    }
                }
            }
        }
        return false;
    }

    public static RazorCodeBehindInfo? GetRazorCodeBehindInfo(GeneratorSyntaxContext context)
    {
        try
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
            {
                return null;
            }

            // Find fields of ObservableModel type and check for ObservableComponent<T> base class
            var observableModelFields = new List<string>();
            var fieldToTypeMap = new Dictionary<string, string>();
            
            // Check if this inherits from ObservableComponent<T>
            var baseModelType = GetObservableComponentBaseModelType(classSymbol);
            if (baseModelType != null)
            {
                // Add "Model" as a virtual field for ObservableComponent<T>
                observableModelFields.Add("Model");
                fieldToTypeMap["Model"] = baseModelType;
            }
            
            // Find explicit fields of ObservableModel type
            foreach (var field in classDecl.Members.OfType<FieldDeclarationSyntax>())
            {
                var firstVariable = field.Declaration.Variables.FirstOrDefault();
                if (firstVariable != null)
                {
                    var fieldSymbol = semanticModel.GetDeclaredSymbol(firstVariable) as IFieldSymbol;
                    if (fieldSymbol?.Type != null && IsObservableModelType(fieldSymbol.Type))
                    {
                        foreach (var variable in field.Declaration.Variables)
                        {
                            var fieldName = variable.Identifier.ValueText;
                            observableModelFields.Add(fieldName);
                            fieldToTypeMap[fieldName] = fieldSymbol.Type.ToDisplayString();
                        }
                    }
                }
            }

            // Check for diagnostic: Has ObservableModel fields but not derived from ObservableComponent or LayoutComponentBase
            var hasObservableModelFields = observableModelFields.Any(f => f != "Model");
            var isObservableComponent = IsObservableComponent(classSymbol);
            var isLayoutComponent = IsLayoutComponent(classSymbol);
            
            if (hasObservableModelFields && !isObservableComponent && !isLayoutComponent)
            {
                // This will be handled by returning a special diagnostic info
                return new RazorCodeBehindInfo(
                    classSymbol.ContainingNamespace.ToDisplayString(),
                    classSymbol.Name,
                    observableModelFields,
                    [],
                    fieldToTypeMap,
                    new Dictionary<string, List<string>>(),
                    true); // HasDiagnosticIssue = true
            }

            if (!observableModelFields.Any()) return null;
            
            // Return basic info - property analysis will be done later with additional texts
            return new RazorCodeBehindInfo(
                classSymbol.ContainingNamespace.ToDisplayString(),
                classSymbol.Name,
                observableModelFields,
                [], // Empty for now, will be filled later
                fieldToTypeMap);
        }
        catch (Exception)
        {
            // Report diagnostic instead of throwing
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var location = classDecl.Identifier.GetLocation();
            // Note: Can't report diagnostic here in static method, will be handled at generator level
            return null;
        }
    }

    public static RazorCodeBehindInfo? AnalyzeRazorWithAdditionalTexts(
        RazorCodeBehindInfo? codeBehindInfo,
        ImmutableArray<AdditionalText> razorFiles,
        ImmutableArray<ObservableModelInfo?> observableModels)
    {
        if (codeBehindInfo == null) return null;

        // Find the corresponding .razor file for this code-behind
        var razorFile = razorFiles.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f.Path) == codeBehindInfo.ClassName);

        if (razorFile == null)
        {
            // Return null instead of throwing - diagnostic will be reported at generator level
            return null;
        }

        try
        {
            var razorContent = razorFile.GetText()?.ToString() ?? string.Empty;
            var (usedProperties, fieldToPropertiesMap) = AnalyzeRazorContent(razorContent,
                codeBehindInfo.ObservableModelFields, observableModels);

            return new RazorCodeBehindInfo(
                codeBehindInfo.Namespace,
                codeBehindInfo.ClassName,
                codeBehindInfo.ObservableModelFields,
                usedProperties,
                codeBehindInfo.FieldToTypeMap,
                fieldToPropertiesMap);
        }
        catch (Exception)
        {
            // Return null on error instead of throwing
            return null;
        }
    }

    private static (List<string> usedProperties, Dictionary<string, List<string>> fieldToPropertiesMap)
        AnalyzeRazorContent(
            string razorContent,
            List<string> modelFields,
            ImmutableArray<ObservableModelInfo?> observableModels)
    {
        try
        {
            var usedProperties = new HashSet<string>();
            var usedCommands = new HashSet<string>();
            var fieldToPropertiesMap = new Dictionary<string, List<string>>();

            // Get all partial properties and command properties from observable models
            var allPartialProperties = new HashSet<string>();
            var allCommandProperties = new HashSet<string>();
            var commandToPropertiesMap = new Dictionary<string, List<string>>();

            foreach (var model in observableModels.Where(m => m != null))
            {
                foreach (var prop in model!.PartialProperties)
                {
                    allPartialProperties.Add(prop.Name);
                }

                foreach (var cmd in model.CommandProperties)
                {
                    allCommandProperties.Add(cmd.Name);
                    commandToPropertiesMap[cmd.Name] = ObservableModelAnalyzer.GetObservedProperties(model, cmd);
                }
            }

            // Analyze razor content for property and command usage
            foreach (var modelField in modelFields)
            {
                fieldToPropertiesMap[modelField] = new List<string>();

                // Pattern: @modelField.PropertyName or modelField.PropertyName
                var propertyPattern = $@"\b{Regex.Escape(modelField)}\.([a-zA-Z_][a-zA-Z0-9_]*)";
                var matches = Regex.Matches(razorContent, propertyPattern);

                foreach (Match match in matches)
                {
                    var memberName = match.Groups[1].Value;

                    if (allPartialProperties.Contains(memberName))
                    {
                        usedProperties.Add(memberName);
                        if (!fieldToPropertiesMap[modelField].Contains(memberName))
                        {
                            fieldToPropertiesMap[modelField].Add(memberName);
                        }
                    }
                    else if (allCommandProperties.Contains(memberName))
                    {
                        usedCommands.Add(memberName);
                        // Add command dependencies to this field's properties
                        if (commandToPropertiesMap.TryGetValue(memberName, out var commandDeps))
                        {
                            foreach (var dep in commandDeps)
                            {
                                if (!fieldToPropertiesMap[modelField].Contains(dep))
                                {
                                    fieldToPropertiesMap[modelField].Add(dep);
                                }
                            }
                        }
                    }
                }
            }

            // Add properties that the used commands depend on (for backward compatibility)
            foreach (var command in usedCommands)
            {
                if (commandToPropertiesMap.TryGetValue(command, out var commandDeps))
                {
                    foreach (var dep in commandDeps)
                    {
                        usedProperties.Add(dep);
                    }
                }
            }

            return (usedProperties.ToList(), fieldToPropertiesMap);
        }
        catch (Exception)
        {
            // Return empty results on analysis error rather than throwing
            return (new List<string>(), new Dictionary<string, List<string>>());
        }
    }

    private static bool IsObservableModelType(ITypeSymbol typeSymbol)
    {
        // Check if it's a class that inherits from ObservableModel
        var currentType = typeSymbol;
        while (currentType != null)
        {
            if (currentType.Name == "ObservableModel")
                return true;
            currentType = currentType.BaseType;
        }

        // Check if it's an interface that inherits from IObservableModel
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var interfaceType in namedTypeSymbol.AllInterfaces)
            {
                if (interfaceType.Name == "IObservableModel")
                    return true;
            }
        }

        return false;
    }

    private static string? GetObservableComponentBaseModelType(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "ObservableComponent" && baseType.TypeArguments.Length == 1)
            {
                return baseType.TypeArguments[0].ToDisplayString();
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    private static bool IsObservableComponent(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "ObservableComponent")
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static bool IsLayoutComponent(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "LayoutComponentBase")
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    /// <summary>
    /// Detects .razor files that inherit from ObservableComponent but don't have a code-behind file.
    /// Returns RazorCodeBehindInfo for these files so they can be processed by the regular pipeline.
    /// </summary>
    public static List<RazorCodeBehindInfo> DetectMissingCodeBehindFiles(
        ImmutableArray<AdditionalText> razorFiles,
        ImmutableArray<RazorCodeBehindInfo?> existingCodeBehinds,
        ImmutableArray<ObservableModelInfo?> observableModels)
    {
        var missingCodeBehinds = new List<RazorCodeBehindInfo>();
        var existingCodeBehindNames = new HashSet<string>(
            existingCodeBehinds.Where(c => c != null).Select(c => c!.ClassName));

        foreach (var razorFile in razorFiles)
        {
            try
            {
                var razorContent = razorFile.GetText()?.ToString() ?? string.Empty;
                var fileName = Path.GetFileNameWithoutExtension(razorFile.Path);

                // Skip if code-behind already exists
                if (existingCodeBehindNames.Contains(fileName))
                {
                    continue;
                }

                // Check if the .razor file has @inherits ObservableComponent<T>
                var inheritsMatch = Regex.Match(razorContent, @"@inherits\s+ObservableComponent<(\S+)>");
                if (inheritsMatch.Success)
                {
                    var modelType = inheritsMatch.Groups[1].Value;
                    var namespaceName = ExtractNamespaceFromPath(razorFile.Path);

                    // Create RazorCodeBehindInfo with Model field (ObservableComponent<T> pattern)
                    var observableModelFields = new List<string> { "Model" };
                    var fieldToTypeMap = new Dictionary<string, string> { { "Model", modelType } };

                    // Analyze razor content for property usage
                    var (usedProperties, fieldToPropertiesMap) = AnalyzeRazorContent(
                        razorContent,
                        observableModelFields,
                        observableModels);

                    missingCodeBehinds.Add(new RazorCodeBehindInfo(
                        namespaceName,
                        fileName,
                        observableModelFields,
                        usedProperties,
                        fieldToTypeMap,
                        fieldToPropertiesMap));
                }
            }
            catch (Exception)
            {
                // Skip files that can't be analyzed
                continue;
            }
        }

        return missingCodeBehinds;
    }

    private static string ExtractNamespaceFromPath(string filePath)
    {
        // Extract namespace from file path
        // Example: /Users/.../RxBlazorV2Sample/Samples/CommandTriggers/Page.razor
        // -> RxBlazorV2Sample.Samples.CommandTriggers

        var segments = filePath.Replace('\\', '/').Split('/');
        var relevantSegments = new List<string>();
        var foundProject = false;

        foreach (var segment in segments)
        {
            if (segment.EndsWith(".csproj") || segment.EndsWith("Sample") || segment.EndsWith("Tests"))
            {
                relevantSegments.Clear();
                relevantSegments.Add(segment.Replace(".csproj", ""));
                foundProject = true;
                continue;
            }

            if (foundProject && !segment.EndsWith(".razor") && !segment.EndsWith(".cs"))
            {
                // This is a directory after the project root
                relevantSegments.Add(segment);
            }
        }

        return relevantSegments.Any() ? string.Join(".", relevantSegments) : "Unknown";
    }
}