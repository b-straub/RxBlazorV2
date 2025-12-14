using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2.GeneratorTests.Helpers;
using RxBlazorV2Generator;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for partial class support - verifying that the generator correctly handles
/// ObservableModel classes split across multiple partial files.
/// These tests verify no errors are produced (diagnostics-only verification).
/// </summary>
public class PartialClassGeneratorTests
{
    /// <summary>
    /// Tests that commands defined in a separate partial file (without base type) are correctly generated.
    /// This is the main scenario that was broken before the partial class fix.
    /// </summary>
    [Fact]
    public async Task PartialClass_CommandsInSeparateFile_NoDiagnostics()
    {
        // Main file with base type and properties
        // lang=csharp
        const string mainFile = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;

            namespace Test
            {
                [ObservableModelScope(ModelScope.Scoped)]
                public partial class TestModel : ObservableModel
                {
                    public partial string Name { get; set; } = string.Empty;
                    public partial int Counter { get; set; }
                }
            }
            """;

        // Commands file - partial class WITHOUT base type (this was the problematic case)
        // lang=csharp
        const string commandsFile = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;

            namespace Test
            {
                public partial class TestModel
                {
                    [ObservableCommand(nameof(Increment))]
                    public partial IObservableCommand IncrementCommand { get; }

                    private void Increment()
                    {
                        Counter++;
                    }
                }
            }
            """;

        await VerifyMultipleSourcesNoDiagnosticsAsync(
            ("TestModel.cs", mainFile),
            ("TestModel.Commands.cs", commandsFile));
    }

    /// <summary>
    /// Tests that trigger methods in a separate partial file are correctly detected and used.
    /// The trigger method is in a separate file, but properties remain in the main file.
    /// </summary>
    [Fact]
    public async Task PartialClass_TriggerMethodInSeparateFile_NoDiagnostics()
    {
        // Main file with properties (including the one with trigger attribute)
        // lang=csharp
        const string mainFile = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;

            namespace Test
            {
                [ObservableModelScope(ModelScope.Scoped)]
                public partial class TestModel : ObservableModel
                {
                    [ObservableTrigger(nameof(ValidateName))]
                    public partial string Name { get; set; } = string.Empty;

                    public partial string? NameError { get; set; }
                }
            }
            """;

        // Triggers file - contains only the trigger method implementation (no attributes needed)
        // lang=csharp
        const string triggersFile = """
            namespace Test
            {
                public partial class TestModel
                {
                    private void ValidateName()
                    {
                        NameError = string.IsNullOrWhiteSpace(Name)
                            ? "Name is required"
                            : null;
                    }
                }
            }
            """;

        await VerifyMultipleSourcesNoDiagnosticsAsync(
            ("TestModel.cs", mainFile),
            ("TestModel.Triggers.cs", triggersFile));
    }

    /// <summary>
    /// Tests that a partial class with three files (properties, commands, methods) all merge correctly.
    /// </summary>
    [Fact]
    public async Task PartialClass_ThreeFiles_AllMergeCorrectly_NoDiagnostics()
    {
        // Main file with base type and properties
        // lang=csharp
        const string mainFile = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;

            namespace Test
            {
                [ObservableModelScope(ModelScope.Scoped)]
                public partial class TestModel : ObservableModel
                {
                    public partial int Value { get; set; }
                }
            }
            """;

        // Commands file
        // lang=csharp
        const string commandsFile = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;

            namespace Test
            {
                public partial class TestModel
                {
                    [ObservableCommand(nameof(Reset), nameof(CanReset))]
                    public partial IObservableCommand ResetCommand { get; }

                    private bool CanReset() => Value != 0;

                    private void Reset()
                    {
                        Value = 0;
                    }
                }
            }
            """;

        // Methods file - public API methods (no attributes, just helper methods)
        // lang=csharp
        const string methodsFile = """
            namespace Test
            {
                public partial class TestModel
                {
                    public void SetValue(int newValue)
                    {
                        Value = newValue;
                    }
                }
            }
            """;

        await VerifyMultipleSourcesNoDiagnosticsAsync(
            ("TestModel.cs", mainFile),
            ("TestModel.Commands.cs", commandsFile),
            ("TestModel.Methods.cs", methodsFile));
    }

    /// <summary>
    /// Tests that async commands with CancellationToken in a separate file work correctly.
    /// </summary>
    [Fact]
    public async Task PartialClass_AsyncCommandWithCancellation_NoDiagnostics()
    {
        // Main file with base type and properties
        // lang=csharp
        const string mainFile = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;

            namespace Test
            {
                [ObservableModelScope(ModelScope.Scoped)]
                public partial class TestModel : ObservableModel
                {
                    public partial bool IsLoading { get; set; }
                    public partial string? Result { get; set; }
                }
            }
            """;

        // Commands file with async command
        // lang=csharp
        const string commandsFile = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Test
            {
                public partial class TestModel
                {
                    [ObservableCommand(nameof(LoadDataAsync), nameof(CanLoadData))]
                    public partial IObservableCommandAsync LoadCommand { get; }

                    private bool CanLoadData() => !IsLoading;

                    private async Task LoadDataAsync(CancellationToken ct)
                    {
                        IsLoading = true;
                        try
                        {
                            await Task.Delay(100, ct);
                            Result = "Loaded";
                        }
                        finally
                        {
                            IsLoading = false;
                        }
                    }
                }
            }
            """;

        await VerifyMultipleSourcesNoDiagnosticsAsync(
            ("TestModel.cs", mainFile),
            ("TestModel.Commands.cs", commandsFile));
    }

    /// <summary>
    /// Tests that model references work correctly with partial files.
    /// </summary>
    [Fact]
    public async Task PartialClass_WithModelReference_NoDiagnostics()
    {
        // Referenced model
        // lang=csharp
        const string referencedModel = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;

            namespace Test
            {
                [ObservableModelScope(ModelScope.Singleton)]
                public partial class SettingsModel : ObservableModel
                {
                    public partial bool DarkMode { get; set; }
                }
            }
            """;

        // Main file with base type, constructor, and properties
        // lang=csharp
        const string mainFile = """
            using RxBlazorV2.Model;
            using RxBlazorV2.Interface;

            namespace Test
            {
                [ObservableModelScope(ModelScope.Scoped)]
                public partial class TestModel : ObservableModel
                {
                    public partial TestModel(SettingsModel settings);

                    public partial string Theme { get; set; } = "light";
                }
            }
            """;

        // Methods file with logic that uses the referenced model
        // lang=csharp
        const string methodsFile = """
            namespace Test
            {
                public partial class TestModel
                {
                    public void ApplySettings()
                    {
                        Theme = Settings.DarkMode ? "dark" : "light";
                    }
                }
            }
            """;

        await VerifyMultipleSourcesNoDiagnosticsAsync(
            ("SettingsModel.cs", referencedModel),
            ("TestModel.cs", mainFile),
            ("TestModel.Methods.cs", methodsFile));
    }

    /// <summary>
    /// Helper method to verify generator produces no errors with multiple source files.
    /// Uses SkipGeneratedSourcesCheck to only verify diagnostics.
    /// </summary>
    private static Task VerifyMultipleSourcesNoDiagnosticsAsync(params (string fileName, string content)[] sources)
    {
        var test = new MultiSourceGeneratorTest();

        // Add all source files
        foreach (var (fileName, content) in sources)
        {
            test.TestState.Sources.Add((fileName, SourceText.From(content, Encoding.UTF8)));
        }

        // Add global usings
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(TestShared.GlobalUsing, Encoding.UTF8)));

        // Skip generated sources verification - we only care that no errors are produced
        test.TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck;

        test.TestState.ReferenceAssemblies = TestShared.ReferenceAssemblies();
        test.TestState.AdditionalReferences.Add(typeof(Model.ObservableModel).Assembly);

        return test.RunAsync();
    }
}

/// <summary>
/// Test class that supports multiple source files for partial class testing.
/// </summary>
internal class MultiSourceGeneratorTest : CSharpSourceGeneratorTest<RxBlazorGenerator, DefaultVerifier>
{
    protected override ParseOptions CreateParseOptions()
    {
        return new CSharpParseOptions(languageVersion: LanguageVersion.Preview);
    }
}
