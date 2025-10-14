using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace RxBlazorV2Generator.Analysis;

/// <summary>
/// Represents a complete analysis record for a Razor code-behind class.
/// This is the single source of truth for Razor component analysis and diagnostics.
/// Follows the DexieNET pattern for clean separation between analysis and diagnostic reporting.
/// </summary>
public class RazorCodeBehindRecord
{
    public RazorCodeBehindInfo CodeBehindInfo { get; }
    private readonly List<Diagnostic> _diagnostics;

    private RazorCodeBehindRecord(RazorCodeBehindInfo codeBehindInfo, List<Diagnostic> diagnostics)
    {
        CodeBehindInfo = codeBehindInfo;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Creates a RazorCodeBehindRecord by analyzing the code-behind class and optionally the .razor file.
    /// Returns null if the class is not a valid razor code-behind or analysis fails.
    /// This is the single place where Razor code-behind analysis happens - used by both analyzer and generator.
    /// </summary>
    public static RazorCodeBehindRecord? Create(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        AdditionalText? razorFile,
        ImmutableArray<ObservableModelInfo?> observableModels)
    {
        try
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return null;
            }

            // Check if this is a Razor code-behind class
            if (!RazorAnalyzer.IsRazorCodeBehindClass(classDecl, semanticModel))
            {
                return null;
            }

            var diagnostics = new List<Diagnostic>();
            var observableModelFields = new List<string>();
            var fieldToTypeMap = new Dictionary<string, string>();
            var injectedProperties = new HashSet<string>();

            // Check if this inherits from ObservableComponent<T>
            var baseModelType = GetObservableComponentBaseModelType(namedTypeSymbol);
            if (baseModelType != null)
            {
                observableModelFields.Add("Model");
                fieldToTypeMap["Model"] = baseModelType;
            }

            // Check if this inherits from OwningComponentBase<T> where T is an ObservableModel
            var owningComponentBaseModelType = GetOwningComponentBaseModelType(namedTypeSymbol);
            if (owningComponentBaseModelType != null)
            {
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
                    var hasInjectAttribute = propertySymbol.GetAttributes().Any(attr =>
                        attr.AttributeClass?.Name == "Inject" ||
                        attr.AttributeClass?.Name == "InjectAttribute");

                    if (hasInjectAttribute)
                    {
                        var propertyName = property.Identifier.ValueText;
                        observableModelFields.Add(propertyName);
                        fieldToTypeMap[propertyName] = propertySymbol.Type.ToDisplayString();
                        injectedProperties.Add(propertyName);
                    }
                }
            }

            // Check for RXBG009 diagnostic: Has ObservableModel fields but not derived from ObservableComponent
            var isObservableComponent = IsObservableComponent(namedTypeSymbol);
            var hasObservableModelFields = isObservableComponent
                ? observableModelFields.Any(f => f != "Model" && f != "Service")
                : observableModelFields.Any();

            if (hasObservableModelFields && !isObservableComponent)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ComponentNotObservableWarning,
                    classDecl.Identifier.GetLocation(),
                    namedTypeSymbol.Name);
                diagnostics.Add(diagnostic);

                // Return diagnostic-only record (no code generation)
                return new RazorCodeBehindRecord(
                    new RazorCodeBehindInfo(
                        namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                        namedTypeSymbol.Name,
                        observableModelFields,
                        [],
                        fieldToTypeMap,
                        new Dictionary<string, List<string>>(),
                        hasDiagnosticIssue: true,
                        codeBehindPropertyAccesses: null,
                        injectedProperties: injectedProperties,
                        isObservableComponent: false),
                    diagnostics);
            }

            if (!observableModelFields.Any())
            {
                return null;
            }

            // Analyze code-behind for property accesses
            var codeBehindPropertyAccesses = AnalyzeCodeBehindPropertyAccesses(
                classDecl,
                semanticModel,
                observableModelFields,
                fieldToTypeMap);

            // If we have a razor file, analyze it for property usage and check inheritance
            List<string> usedProperties = [];
            Dictionary<string, List<string>> fieldToPropertiesMap = new();

            if (razorFile != null)
            {
                var razorContent = razorFile.GetText()?.ToString() ?? string.Empty;
                (usedProperties, fieldToPropertiesMap) = AnalyzeRazorContent(
                    razorContent,
                    observableModelFields,
                    observableModels);

                // Merge code-behind property accesses with razor file property accesses
                foreach (var kvp in codeBehindPropertyAccesses)
                {
                    if (!fieldToPropertiesMap.ContainsKey(kvp.Key))
                    {
                        fieldToPropertiesMap[kvp.Key] = new List<string>();
                    }

                    foreach (var property in kvp.Value)
                    {
                        if (!fieldToPropertiesMap[kvp.Key].Contains(property))
                        {
                            fieldToPropertiesMap[kvp.Key].Add(property);
                        }
                    }
                }

                // Check for RXBG019: .razor file inheritance mismatch
                var (hasMatch, expectedInherits) = CheckRazorInheritanceMatch(
                    namedTypeSymbol,
                    razorContent);

                if (!hasMatch && expectedInherits != null)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.RazorInheritanceMismatchWarning,
                        classDecl.Identifier.GetLocation(),
                        namedTypeSymbol.Name,
                        expectedInherits);
                    diagnostics.Add(diagnostic);
                }
            }

            var codeBehindInfo = new RazorCodeBehindInfo(
                namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                namedTypeSymbol.Name,
                observableModelFields,
                usedProperties,
                fieldToTypeMap,
                fieldToPropertiesMap.Any() ? fieldToPropertiesMap : null,
                hasDiagnosticIssue: false,
                codeBehindPropertyAccesses: razorFile == null ? codeBehindPropertyAccesses : null,
                injectedProperties: injectedProperties);

            return new RazorCodeBehindRecord(codeBehindInfo, diagnostics);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a RazorCodeBehindRecord from a .razor file without a code-behind class.
    /// Used for .razor files with @inherits ObservableComponent or @inject directives.
    /// </summary>
    public static RazorCodeBehindRecord? CreateFromRazorFile(
        AdditionalText razorFile,
        ImmutableArray<ObservableModelInfo?> observableModels)
    {
        try
        {
            var razorContent = razorFile.GetText()?.ToString() ?? string.Empty;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(razorFile.Path);
            var namespaceName = RazorContentParser.ExtractNamespaceFromPath(razorFile.Path);

            var observableModelFields = new List<string>();
            var fieldToTypeMap = new Dictionary<string, string>();
            var injectedProperties = new HashSet<string>();

            // Check for @inherits ObservableComponent<T>
            var modelType = RazorContentParser.ParseInheritsObservableComponent(razorContent);
            if (modelType != null)
            {
                observableModelFields.Add("Model");
                fieldToTypeMap["Model"] = modelType;
            }

            // Check for @inject directives with ObservableModel types
            var typeNameToFullName = RazorContentParser.BuildTypeNameMap(observableModels);
            var injectDirectives = RazorContentParser.ParseInjectDirectives(razorContent);

            foreach (var (typeName, fieldName) in injectDirectives)
            {
                var simpleTypeName = RazorContentParser.GetSimpleTypeName(typeName);

                if (typeNameToFullName.TryGetValue(simpleTypeName, out var fullTypeName))
                {
                    observableModelFields.Add(fieldName);
                    fieldToTypeMap[fieldName] = fullTypeName;
                    injectedProperties.Add(fieldName);
                }
            }

            if (!observableModelFields.Any())
            {
                return null;
            }

            var diagnostics = new List<Diagnostic>();
            var hasObservableComponentInheritance = RazorContentParser.HasObservableComponentInheritance(razorContent);

            // If we have ObservableModel @inject directives but no ObservableComponent inheritance, report RXBG009
            var hasDiagnosticIssue = !hasObservableComponentInheritance;
            if (hasDiagnosticIssue)
            {
                // Note: We can't create a proper diagnostic here without a location in the razor file
                // The generator will need to handle this case
            }

            // Analyze razor content for property usage
            var (usedProperties, fieldToPropertiesMap) = AnalyzeRazorContent(
                razorContent,
                observableModelFields,
                observableModels);

            var codeBehindInfo = new RazorCodeBehindInfo(
                namespaceName,
                fileName,
                observableModelFields,
                usedProperties,
                fieldToTypeMap,
                fieldToPropertiesMap,
                hasDiagnosticIssue,
                null,
                injectedProperties,
                hasObservableComponentInheritance);

            return new RazorCodeBehindRecord(codeBehindInfo, diagnostics);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns all diagnostics for this razor code-behind.
    /// </summary>
    public List<Diagnostic> Verify()
    {
        return new List<Diagnostic>(_diagnostics);
    }

    private static Dictionary<string, List<string>> AnalyzeCodeBehindPropertyAccesses(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        List<string> modelFields,
        Dictionary<string, string> fieldToTypeMap)
    {
        var fieldToPropertiesMap = new Dictionary<string, List<string>>();

        foreach (var field in modelFields)
        {
            fieldToPropertiesMap[field] = new List<string>();
        }

        foreach (var identifier in classDecl.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var identifierText = identifier.Identifier.ValueText;

            if (modelFields.Contains(identifierText))
            {
                if (identifier.Parent is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression == identifier)
                {
                    var propertyName = memberAccess.Name.Identifier.ValueText;

                    if (!fieldToPropertiesMap[identifierText].Contains(propertyName))
                    {
                        fieldToPropertiesMap[identifierText].Add(propertyName);
                    }
                }
            }
        }

        return fieldToPropertiesMap;
    }

    private static (List<string> usedProperties, Dictionary<string, List<string>> fieldToPropertiesMap)
        AnalyzeRazorContent(
            string razorContent,
            List<string> modelFields,
            ImmutableArray<ObservableModelInfo?> observableModels)
    {
        var usedProperties = new HashSet<string>();
        var usedCommands = new HashSet<string>();
        var fieldToPropertiesMap = new Dictionary<string, List<string>>();

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

        foreach (var modelField in modelFields)
        {
            fieldToPropertiesMap[modelField] = new List<string>();

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

    private static bool IsObservableModelType(ITypeSymbol typeSymbol)
    {
        var currentType = typeSymbol;
        while (currentType != null)
        {
            if (currentType.Name == "ObservableModel")
            {
                return true;
            }
            currentType = currentType.BaseType;
        }

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var interfaceType in namedTypeSymbol.AllInterfaces)
            {
                if (interfaceType.Name == "IObservableModel")
                {
                    return true;
                }
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

    private static string? GetOwningComponentBaseModelType(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "OwningComponentBase" && baseType.TypeArguments.Length == 1)
            {
                var typeArg = baseType.TypeArguments[0];
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
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static (bool hasMatch, string? expectedInherits) CheckRazorInheritanceMatch(
        INamedTypeSymbol classSymbol,
        string razorContent)
    {
        var (isObservableComponent, baseTypeName) = GetObservableComponentInheritanceInfo(classSymbol);
        if (!isObservableComponent || baseTypeName == null)
        {
            return (true, null);
        }

        if (HasMatchingInheritsDirective(razorContent, baseTypeName))
        {
            return (true, null);
        }

        return (false, baseTypeName);
    }

    private static (bool isObservableComponent, string? baseTypeName) GetObservableComponentInheritanceInfo(
        INamedTypeSymbol classSymbol)
    {
        var directBaseType = classSymbol.BaseType;
        if (directBaseType == null)
        {
            return (false, null);
        }

        var checkType = directBaseType;
        var isObservableComponent = false;
        while (checkType != null)
        {
            if (checkType.Name == "ObservableComponent" || checkType.Name == "ObservableLayoutComponentBase")
            {
                isObservableComponent = true;
                break;
            }
            checkType = checkType.BaseType;
        }

        if (!isObservableComponent)
        {
            return (false, null);
        }

        if (directBaseType.TypeArguments.Length == 1)
        {
            var modelType = directBaseType.TypeArguments[0].ToDisplayString();
            return (true, $"{directBaseType.Name}<{modelType}>");
        }

        return (true, directBaseType.Name);
    }

    private static bool HasMatchingInheritsDirective(string razorContent, string expectedBaseTypeName)
    {
        var inheritsMatches = Regex.Matches(razorContent, @"@inherits\s+([^\s\r\n]+)", RegexOptions.Multiline);

        foreach (Match inheritsMatch in inheritsMatches)
        {
            var inheritsTypeName = inheritsMatch.Groups[1].Value;

            if (IsMatchingType(inheritsTypeName, expectedBaseTypeName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMatchingType(string actualTypeName, string expectedTypeName)
    {
        if (actualTypeName == expectedTypeName)
        {
            return true;
        }

        var actualParts = ParseTypeName(actualTypeName);
        var expectedParts = ParseTypeName(expectedTypeName);

        if (actualParts == null || expectedParts == null)
        {
            return false;
        }

        var actualBaseName = actualParts.Value.baseName.Split('.').Last();
        var expectedBaseName = expectedParts.Value.baseName.Split('.').Last();

        if (actualBaseName != expectedBaseName)
        {
            return false;
        }

        if (actualParts.Value.typeArgs == null && expectedParts.Value.typeArgs == null)
        {
            return true;
        }

        if (actualParts.Value.typeArgs == null || expectedParts.Value.typeArgs == null)
        {
            return false;
        }

        var actualTypeArgShort = actualParts.Value.typeArgs.Split('.').Last();
        var expectedTypeArgShort = expectedParts.Value.typeArgs.Split('.').Last();

        return actualTypeArgShort == expectedTypeArgShort;
    }

    private static (string baseName, string? typeArgs)? ParseTypeName(string typeName)
    {
        if (typeName.Contains("<"))
        {
            var match = Regex.Match(typeName, @"^([^<]+)<(.+)>$");
            if (match.Success)
            {
                return (match.Groups[1].Value, match.Groups[2].Value);
            }
            return null;
        }

        return (typeName, null);
    }
}
