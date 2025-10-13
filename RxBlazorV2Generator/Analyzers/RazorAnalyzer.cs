using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
                          typeName.Contains("LayoutComponentBase");
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
                    // Check if this type or any of its base types are ObservableComponent, ComponentBase, OwningComponentBase, or LayoutComponentBase
                    var currentType = baseTypeSymbol;
                    while (currentType != null)
                    {
                        var fullName = currentType.ToDisplayString();
                        if (fullName.Contains("ObservableComponent") ||
                            fullName.Contains("ComponentBase") ||
                            fullName.Contains("OwningComponentBase") ||
                            fullName.Contains("LayoutComponentBase"))
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
        var classDecl = (ClassDeclarationSyntax)context.Node;
        return GetRazorCodeBehindInfo(classDecl, context.SemanticModel);
    }

    public static RazorCodeBehindInfo? GetRazorCodeBehindInfo(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        try
        {

            if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
            {
                return null;
            }

            // Find fields of ObservableModel type and check for ObservableComponent<T> base class
            var observableModelFields = new List<string>();
            var fieldToTypeMap = new Dictionary<string, string>();
            var injectedProperties = new HashSet<string>();

            // Check if this inherits from ObservableComponent<T>
            var baseModelType = GetObservableComponentBaseModelType(classSymbol);
            if (baseModelType != null)
            {
                // Add "Model" as a virtual field for ObservableComponent<T>
                observableModelFields.Add("Model");
                fieldToTypeMap["Model"] = baseModelType;
            }

            // Check if this inherits from OwningComponentBase<T> where T is an ObservableModel
            var owningComponentBaseModelType = GetOwningComponentBaseModelType(classSymbol, semanticModel);
            if (owningComponentBaseModelType != null)
            {
                // Add "Service" as a virtual field for OwningComponentBase<T>
                observableModelFields.Add("Service");
                fieldToTypeMap["Service"] = owningComponentBaseModelType;
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

            // Find [Inject] properties of ObservableModel type
            foreach (var property in classDecl.Members.OfType<PropertyDeclarationSyntax>())
            {
                var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;
                if (propertySymbol?.Type != null && IsObservableModelType(propertySymbol.Type))
                {
                    // Check if it has [Inject] attribute
                    var hasInjectAttribute = propertySymbol.GetAttributes().Any(attr =>
                        attr.AttributeClass?.Name == "Inject" ||
                        attr.AttributeClass?.Name == "InjectAttribute");

                    if (hasInjectAttribute)
                    {
                        var propertyName = property.Identifier.ValueText;
                        observableModelFields.Add(propertyName);
                        fieldToTypeMap[propertyName] = propertySymbol.Type.ToDisplayString();
                        injectedProperties.Add(propertyName); // Track as injected property
                    }
                }
            }

            // Check for diagnostic: Has ObservableModel fields but not derived from ObservableComponent
            var isObservableComponent = IsObservableComponent(classSymbol);
            var isComponentBase = IsComponentBase(classSymbol);
            var isLayoutComponent = IsLayoutComponent(classSymbol);

            // For diagnostic purposes:
            // - If it's ObservableComponent, check for fields OTHER than the virtual "Model" or "Service" properties
            // - If it's not ObservableComponent, check for ANY ObservableModel fields (including injected "Model" or "Service")
            var hasObservableModelFields = isObservableComponent
                ? observableModelFields.Any(f => f != "Model" && f != "Service")
                : observableModelFields.Any();

            // Report diagnostic for any component with ObservableModel fields that doesn't inherit from ObservableComponent
            // ComponentBase: Can be changed to ObservableComponent (code fix available)
            // LayoutComponentBase/OwningComponentBase: Cannot be changed (user must handle manually)
            if (hasObservableModelFields && !isObservableComponent)
            {
                // Return diagnostic info but no code generation
                return new RazorCodeBehindInfo(
                    classSymbol.ContainingNamespace.ToDisplayString(),
                    classSymbol.Name,
                    observableModelFields,
                    [],
                    fieldToTypeMap,
                    new Dictionary<string, List<string>>(),
                    hasDiagnosticIssue: true, // Report diagnostic
                    codeBehindPropertyAccesses: null,
                    injectedProperties: injectedProperties,
                    isObservableComponent: false); // Not an ObservableComponent
            }

            if (!observableModelFields.Any()) return null;

            // Analyze code-behind for property accesses, passing the fieldToTypeMap
            var codeBehindPropertyAccesses = AnalyzeCodeBehindPropertyAccesses(classDecl, semanticModel, observableModelFields, fieldToTypeMap);

            // Return basic info - razor file property analysis will be done later with additional texts
            return new RazorCodeBehindInfo(
                classSymbol.ContainingNamespace.ToDisplayString(),
                classSymbol.Name,
                observableModelFields,
                [], // Empty for now, will be filled later from razor file
                fieldToTypeMap,
                null, // fieldToPropertiesMap will be merged later
                false,
                codeBehindPropertyAccesses,
                injectedProperties);
        }
        catch (Exception)
        {
            // Report diagnostic instead of throwing
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
            // No .razor file found - return code-behind info as-is
            // This can happen for pure code-behind components or when .razor file is in a different location
            return codeBehindInfo;
        }

        try
        {
            var razorContent = razorFile.GetText()?.ToString() ?? string.Empty;
            var (usedProperties, fieldToPropertiesMap) = AnalyzeRazorContent(razorContent,
                codeBehindInfo.ObservableModelFields, observableModels);

            // Merge code-behind property accesses with razor file property accesses
            var mergedFieldToPropertiesMap = new Dictionary<string, List<string>>(fieldToPropertiesMap);
            foreach (var kvp in codeBehindInfo.CodeBehindPropertyAccesses)
            {
                if (!mergedFieldToPropertiesMap.ContainsKey(kvp.Key))
                {
                    mergedFieldToPropertiesMap[kvp.Key] = new List<string>();
                }

                foreach (var property in kvp.Value)
                {
                    if (!mergedFieldToPropertiesMap[kvp.Key].Contains(property))
                    {
                        mergedFieldToPropertiesMap[kvp.Key].Add(property);
                    }
                }
            }

            return new RazorCodeBehindInfo(
                codeBehindInfo.Namespace,
                codeBehindInfo.ClassName,
                codeBehindInfo.ObservableModelFields,
                usedProperties,
                codeBehindInfo.FieldToTypeMap,
                mergedFieldToPropertiesMap,
                codeBehindInfo.HasDiagnosticIssue,
                null,
                codeBehindInfo.InjectedProperties);
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

    /// <summary>
    /// Analyzes code-behind class for property accesses on observable model fields using syntax-only analysis.
    /// Returns a dictionary mapping field names to lists of accessed property names.
    /// This uses IdentifierNameSyntax to cover all access patterns including
    /// expression-bodied properties, methods, and fields without requiring semantic analysis.
    /// </summary>
    private static Dictionary<string, List<string>> AnalyzeCodeBehindPropertyAccesses(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        List<string> modelFields,
        Dictionary<string, string> fieldToTypeMap)
    {
        var fieldToPropertiesMap = new Dictionary<string, List<string>>();

        try
        {
            // Initialize dictionary for all model fields
            foreach (var field in modelFields)
            {
                fieldToPropertiesMap[field] = new List<string>();
            }

            // Analyze all descendant nodes looking for identifiers matching our model fields
            foreach (var identifier in classDecl.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var identifierText = identifier.Identifier.ValueText;

                // Check if this identifier matches one of our model fields
                if (modelFields.Contains(identifierText))
                {
                    // Check if this identifier is part of a member access expression (e.g., Model.Property)
                    if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Expression == identifier)
                    {
                        var propertyName = memberAccess.Name.Identifier.ValueText;

                        // Add to the map if not already there
                        if (!fieldToPropertiesMap[identifierText].Contains(propertyName))
                        {
                            fieldToPropertiesMap[identifierText].Add(propertyName);
                        }
                    }
                }
            }

            return fieldToPropertiesMap;
        }
        catch (Exception)
        {
            // Return empty results on analysis error
            return fieldToPropertiesMap;
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

    private static string? GetOwningComponentBaseModelType(INamedTypeSymbol classSymbol, SemanticModel semanticModel)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "OwningComponentBase" && baseType.TypeArguments.Length == 1)
            {
                var typeArg = baseType.TypeArguments[0];
                // Check if the type argument is an ObservableModel
                if (IsObservableModelType(typeArg))
                {
                    return typeArg.ToDisplayString();
                }
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
            if (baseType.Name == "ObservableComponent" || baseType.Name == "ObservableLayoutComponentBase")
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static bool IsComponentBase(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "ComponentBase")
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
    /// Also detects .razor files with @inject directives for ObservableModel types.
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

        // Build a map of type names to full type names from observable models
        var typeNameToFullName = new Dictionary<string, string>();
        foreach (var model in observableModels.Where(m => m != null))
        {
            var simpleTypeName = model!.ClassName;
            var fullTypeName = model.FullyQualifiedName;
            typeNameToFullName[simpleTypeName] = fullTypeName;
        }

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

                var observableModelFields = new List<string>();
                var fieldToTypeMap = new Dictionary<string, string>();
                var injectedProperties = new HashSet<string>();
                var namespaceName = ExtractNamespaceFromPath(razorFile.Path);

                // Check if the .razor file has @inherits ObservableComponent<T>
                // Use .+? to handle generic types with multiple type parameters (e.g., GenericModel<string, int>)
                var inheritsMatch = Regex.Match(razorContent, @"@inherits\s+\S*ObservableComponent<(.+?)>\s*$", RegexOptions.Multiline);
                if (inheritsMatch.Success)
                {
                    var modelType = inheritsMatch.Groups[1].Value;

                    // Create RazorCodeBehindInfo with Model field (ObservableComponent<T> pattern)
                    observableModelFields.Add("Model");
                    fieldToTypeMap["Model"] = modelType;
                }

                // Check for @inject directives with ObservableModel types
                var injectMatches = Regex.Matches(razorContent, @"@inject\s+([a-zA-Z_][a-zA-Z0-9_\.]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)");
                foreach (Match injectMatch in injectMatches)
                {
                    var typeName = injectMatch.Groups[1].Value;
                    var fieldName = injectMatch.Groups[2].Value;

                    // Check if this type is an ObservableModel (by simple name or full name)
                    var simpleTypeName = typeName.Contains('.') ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;

                    if (typeNameToFullName.TryGetValue(simpleTypeName, out var fullTypeName))
                    {
                        observableModelFields.Add(fieldName);
                        fieldToTypeMap[fieldName] = fullTypeName;
                        injectedProperties.Add(fieldName); // Track as injected property
                    }
                }

                // Only create RazorCodeBehindInfo if we found any ObservableModel fields
                if (observableModelFields.Any())
                {
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
                        fieldToPropertiesMap,
                        false,
                        null,
                        injectedProperties));
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