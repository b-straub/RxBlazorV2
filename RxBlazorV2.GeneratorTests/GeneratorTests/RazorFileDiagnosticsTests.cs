using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2.GeneratorTests.Helpers;
using RxBlazorV2Generator;
using RxBlazorV2Generator.Diagnostics;
using System.Text;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for Razor file-based diagnostics (RXBG060, RXBG061, RXBG014)
/// These tests verify diagnostics that are reported based on razor file content
/// </summary>
public class RazorFileDiagnosticsTests
{
    /// <summary>
    /// Helper method to generate expected Filter() code-behind content.
    /// Filter properties should use "Model." prefix (e.g., "Model.Name", "Model.Settings.IsDay").
    /// </summary>
    private static string GenerateFilterCodeBehind(string componentName, string baseClass, string namespaceName, string[] filterProperties, string[]? usingDirectives = null)
    {
        var sb = new StringBuilder();

        // Add using directives
        if (usingDirectives != null && usingDirectives.Length > 0)
        {
            foreach (var usingDirective in usingDirectives.OrderBy(u => u))
            {
                sb.AppendLine($"using {usingDirective};");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {componentName} : {baseClass}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Auto-generated filter method for ObservableComponent property filtering.");
        sb.AppendLine("    /// Only properties used in this component trigger re-renders.");
        sb.AppendLine("    /// Property names match the qualified names emitted by Observable streams (ClassName.PropertyName).");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    protected override string[] Filter()");
        sb.AppendLine("    {");

        if (filterProperties != null && filterProperties.Length > 0)
        {
            sb.AppendLine("        return [");
            for (int i = 0; i < filterProperties.Length; i++)
            {
                var comma = i < filterProperties.Length - 1 ? "," : "";
                sb.AppendLine($"            \"{filterProperties[i]}\"{comma}");
            }
            sb.AppendLine("        ];");
        }
        else
        {
            sb.AppendLine("        return [];");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
    [Fact]
    public async Task DirectInheritanceFromObservableComponent_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // Razor file that directly inherits from ObservableComponent<T>
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            {|#0:@inherits ObservableComponent<TestModel>|}

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.DirectObservableComponentInheritanceError)
            .WithLocation(0)
            .WithArguments("Weather", "<TestModel>");

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            modelName: "TestModel",
            expected: expected);
    }

    [Fact]
    public async Task DirectInheritanceFromObservableComponentWithoutGeneric_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // Razor file that directly inherits from ObservableComponent (no generic)
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            {|#0:@inherits ObservableComponent|}

            <h3>Weather</h3>
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.DirectObservableComponentInheritanceError)
            .WithLocation(0)
            .WithArguments("Weather", "");

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            modelName: "TestModel",
            expected: expected);
    }

    [Fact]
    public async Task GeneratedComponentInheritance_NoError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Razor file that inherits from generated component with @page - no error expected
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """
        };

        // Expected code-behind generation
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            additionalGeneratedSources: additionalGeneratedSources);
    }

    [Fact]
    public async Task ScopedModelUsedInMultipleRazorFiles_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Multiple Razor files using the same scoped model - should report error in ALL files
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            {|#0:@inherits TestModelComponent|}

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Counter.razor"] = """
            @page "/counter"
            {|#1:@inherits TestModelComponent|}

            <h3>Counter</h3>
            <p>@Model.Name</p>
            """
        };

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.SharedModelNotSingletonError)
                .WithLocation(0)
                .WithArguments("Test.TestModel", "Scoped"),
            new DiagnosticResult(DiagnosticDescriptors.SharedModelNotSingletonError)
                .WithLocation(1)
                .WithArguments("Test.TestModel", "Scoped")
        };

        // Expected code-behind generation (alphabetical order by component name to match generator output)
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Counter.g.cs"] = GenerateFilterCodeBehind(
                "Counter",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" }),
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Scoped",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task TransientModelUsedInMultipleRazorFiles_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Transient)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial int Count { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial int Count
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Count");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Multiple Razor files using the same transient model - should report error in ALL files
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Page1.razor"] = """
            @page "/page1"
            {|#0:@inherits TestModelComponent|}

            <h3>Page 1</h3>
            <p>Count: @Model.Count</p>
            """,
            ["Pages/Page2.razor"] = """
            @page "/page2"
            {|#1:@inherits TestModelComponent|}

            <h3>Page 2</h3>
            <p>Count: @Model.Count</p>
            """,
            ["Pages/Page3.razor"] = """
            @page "/page3"
            {|#2:@inherits TestModelComponent|}

            <h3>Page 3</h3>
            <p>Count: @Model.Count</p>
            """
        };

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.SharedModelNotSingletonError)
                .WithLocation(0)
                .WithArguments("Test.TestModel", "Transient"),
            new DiagnosticResult(DiagnosticDescriptors.SharedModelNotSingletonError)
                .WithLocation(1)
                .WithArguments("Test.TestModel", "Transient"),
            new DiagnosticResult(DiagnosticDescriptors.SharedModelNotSingletonError)
                .WithLocation(2)
                .WithArguments("Test.TestModel", "Transient")
        };

        // Expected code-behind generation
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Page1.g.cs"] = GenerateFilterCodeBehind(
                "Page1",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Count" }, new[] { "Test" }),
            ["Page2.g.cs"] = GenerateFilterCodeBehind(
                "Page2",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Count" }, new[] { "Test" }),
            ["Page3.g.cs"] = GenerateFilterCodeBehind(
                "Page3",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Count" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Transient",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task SingletonModelUsedInMultipleRazorFiles_NoError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Multiple Razor files using the same singleton model - no error expected
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Counter.razor"] = """
            @page "/counter"
            @inherits TestModelComponent

            <h3>Counter</h3>
            <p>@Model.Name</p>
            """
        };

        // Expected code-behind generation (alphabetical order to match generator output)
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Counter.g.cs"] = GenerateFilterCodeBehind(
                "Counter",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" }),
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
            additionalGeneratedSources: additionalGeneratedSources);
    }

    [Fact]
    public async Task ScopedModelUsedInSingleRazorFile_NoError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Single Razor file using scoped model - no error
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """
        };

        // Expected code-behind generation
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Scoped",
            additionalGeneratedSources: additionalGeneratedSources);
    }

    [Fact]
    public async Task RazorWithCodebehindFile_DirectInheritance_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // Razor file with codebehind that directly inherits from ObservableComponent<T>
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            {|#0:@inherits ObservableComponent<TestModel>|}

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Weather.razor.cs"] = """
            using RxBlazorV2.Component;
            using Test;

            namespace Test.Pages
            {
                public partial class Weather : ObservableComponent<TestModel>
                {
                    protected override void OnInitialized()
                    {
                        Model.Name = "Test Weather";
                    }
                }
            }
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.DirectObservableComponentInheritanceError)
            .WithLocation(0)
            .WithArguments("Weather", "<TestModel>");

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            modelName: "TestModel",
            expected: expected);
    }

    [Fact]
    public async Task RazorWithCodebehindFile_ScopedModelInMultipleFiles_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Scoped)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Multiple Razor files with codebehind using the same scoped model - should report error in ALL files
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            {|#0:@inherits TestModelComponent|}

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Weather.razor.cs"] = """
            using Test;

            namespace Test.Pages
            {
                public partial class Weather : TestModelComponent
                {
                    protected override void OnInitialized()
                    {
                        Model.Name = "Test Weather";
                    }
                }
            }
            """,
            ["Pages/Counter.razor"] = """
            @page "/counter"
            {|#1:@inherits TestModelComponent|}

            <h3>Counter</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Counter.razor.cs"] = """
            using Test;

            namespace Test.Pages
            {
                public partial class Counter : TestModelComponent
                {
                    protected override void OnInitialized()
                    {
                        Model.Name = "Test Counter";
                    }
                }
            }
            """
        };

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.SharedModelNotSingletonError)
                .WithLocation(0)
                .WithArguments("Test.TestModel", "Scoped"),
            new DiagnosticResult(DiagnosticDescriptors.SharedModelNotSingletonError)
                .WithLocation(1)
                .WithArguments("Test.TestModel", "Scoped")
        };

        // Expected code-behind generation (alphabetical order to match generator output)
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Counter.g.cs"] = GenerateFilterCodeBehind(
                "Counter",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" }),
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Scoped",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task GeneratedComponentWithPageDirective_NoError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Razor file with @page directive - should NOT report RXBG061
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """
        };

        // Expected code-behind generation
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Name" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            additionalGeneratedSources: additionalGeneratedSources);
    }

    [Fact]
    public async Task GeneratedComponentWithoutPageDirective_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Razor file without @page directive - SHOULD report RXBG061 when used
        // Add a parent page that uses the Widget to trigger same-assembly composition
        var razorFiles = new Dictionary<string, string>
        {
            ["Components/Widget.razor"] = """
            {|#0:@inherits TestModelComponent|}

            <div class="widget">
                <h3>@Model.Name</h3>
            </div>
            """,
            ["Pages/HomePage.razor"] = """
            @page "/home"

            <h1>Home Page</h1>
            <Widget />
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
            .WithLocation(0)
            .WithArguments("Widget.razor", "TestModelComponent");

        // Expected code-behind generation
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Widget.g.cs"] = GenerateFilterCodeBehind(
                "Widget",
                "TestModelComponent",
                "Components",
                new[] { "Model.Name" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task MultipleGeneratedComponentsWithoutPageDirective_ReportsMultipleErrors()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Multiple razor files without @page directive - all should report RXBG061 when used
        // Add a parent page that uses all three components
        var razorFiles = new Dictionary<string, string>
        {
            ["Components/Widget.razor"] = """
            {|#0:@inherits TestModelComponent|}

            <div class="widget">
                <h3>@Model.Name</h3>
            </div>
            """,
            ["Components/Header.razor"] = """
            {|#1:@inherits TestModelComponent|}

            <header>
                <h1>@Model.Name</h1>
            </header>
            """,
            ["Components/Footer.razor"] = """
            {|#2:@inherits TestModelComponent|}

            <footer>
                <p>@Model.Name</p>
            </footer>
            """,
            ["Pages/MainPage.razor"] = """
            @page "/main"

            <Header />
            <Widget />
            <Footer />
            """
        };

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
                .WithLocation(0)
                .WithArguments("Widget.razor", "TestModelComponent"),
            new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
                .WithLocation(1)
                .WithArguments("Header.razor", "TestModelComponent"),
            new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
                .WithLocation(2)
                .WithArguments("Footer.razor", "TestModelComponent")
        };

        // Expected code-behind generation (alphabetical order to match generator output)
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Footer.g.cs"] = GenerateFilterCodeBehind(
                "Footer",
                "TestModelComponent",
                "Components",
                new[] { "Model.Name" }, new[] { "Test" }),
            ["Header.g.cs"] = GenerateFilterCodeBehind(
                "Header",
                "TestModelComponent",
                "Components",
                new[] { "Model.Name" }, new[] { "Test" }),
            ["Widget.g.cs"] = GenerateFilterCodeBehind(
                "Widget",
                "TestModelComponent",
                "Components",
                new[] { "Model.Name" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task MixedPageAndComponentUsage_ReportsErrorOnlyForNonPages()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Title { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Title
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Title");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Mix of pages (with @page) and components (without @page)
        var razorFiles = new Dictionary<string, string>
        {
            // Page - should NOT report error
            ["Pages/Dashboard.razor"] = """
            @page "/dashboard"
            @inherits TestModelComponent

            <h1>Dashboard</h1>
            <p>@Model.Title</p>
            <Sidebar />
            """,
            // Page - should NOT report error
            ["Pages/Settings.razor"] = """
            @page "/settings"
            @inherits TestModelComponent

            <h1>Settings</h1>
            <p>@Model.Title</p>
            """,
            // Component - SHOULD report error when used in same assembly
            ["Components/Sidebar.razor"] = """
            {|#0:@inherits TestModelComponent|}

            <aside>
                <h2>@Model.Title</h2>
            </aside>
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
            .WithLocation(0)
            .WithArguments("Sidebar.razor", "TestModelComponent");

        // Expected code-behind generation (alphabetical order to match generator output)
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Dashboard.g.cs"] = GenerateFilterCodeBehind(
                "Dashboard",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Title" }, new[] { "Test" }),
            ["Settings.g.cs"] = GenerateFilterCodeBehind(
                "Settings",
                "TestModelComponent",
                "Pages",
                new[] { "Model.Title" }, new[] { "Test" }),
            ["Sidebar.g.cs"] = GenerateFilterCodeBehind(
                "Sidebar",
                "TestModelComponent",
                "Components",
                new[] { "Model.Title" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task FullyQualifiedGeneratedComponent_WithoutPageDirective_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                public partial string Data { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Data
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Data");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Razor file with fully-qualified component name without @page - SHOULD report RXBG061 when used
        var razorFiles = new Dictionary<string, string>
        {
            ["Components/Composed.razor"] = """
            {|#0:@inherits Test.TestModelComponent|}

            <div>Data: @Model.Data</div>
            """,
            ["Pages/TestPage.razor"] = """
            @page "/test"

            <Composed />
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
            .WithLocation(0)
            .WithArguments("Composed.razor", "TestModelComponent");

        // Expected code-behind generation - preserves fully-qualified name from razor file
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Composed.g.cs"] = GenerateFilterCodeBehind(
                "Composed",
                "Test.TestModelComponent",  // Fully-qualified as used in razor file
                "Components",
                new[] { "Model.Data" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task CustomNamedGeneratedComponent_WithoutPageDirective_ReportsError()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent("CustomComponent")]
            public partial class TestModel : ObservableModel
            {
                public partial string Data { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial string Data
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Data");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class CustomComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Razor file without @page using custom-named component - SHOULD report RXBG061 when used
        var razorFiles = new Dictionary<string, string>
        {
            ["Components/MyWidget.razor"] = """
            {|#0:@inherits CustomComponent|}

            <div>
                <p>@Model.Data</p>
            </div>
            """,
            ["Pages/ViewPage.razor"] = """
            @page "/view"

            <MyWidget />
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
            .WithLocation(0)
            .WithArguments("MyWidget.razor", "CustomComponent");

        // Expected code-behind generation
        // Note: Always uses "Model." prefix regardless of component name
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["MyWidget.g.cs"] = GenerateFilterCodeBehind(
                "MyWidget",
                "CustomComponent",
                "Components",
                new[] { "Model.Data" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "CustomComponent",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task GeneratedComponentWithoutPageDirective_NotUsedInAssembly_NoError()
    {
        // This test validates the fix for: components defined in assembly A but not used/rendered
        // in assembly A can be safely consumed from assembly B without triggering RXBG061.
        // Real-world example: RxBlazorVSSampleComponents/ErrorManager/ErrorDisplay.razor

        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent]
            public partial class ErrorModel : ObservableModel
            {
                public partial string Message { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generatedModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ErrorModel
        {
            public override string ModelID => "Test.ErrorModel";

            public partial string Message
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Message");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class ErrorModelComponent : ObservableComponent<ErrorModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes - respects Filter() method
                var filter = Filter();
                if (filter.Length == 0)
                {
                    // No filter - observe all property changes
                    Subscriptions.Add(Model.Observable
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
                else
                {
                    // Filter active - observe only filtered properties
                    Subscriptions.Add(Model.Observable
                        .Where(changedProps => changedProps.Intersect(filter).Any())
                        .Chunk(TimeSpan.FromMilliseconds(100))
                        .Subscribe(chunks =>
                        {
                            InvokeAsync(StateHasChanged);
                        }));
                }
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Component without @page directive that is NOT used anywhere in the same assembly
        // Should NOT report RXBG061 because it's safe to use from another assembly
        var razorFiles = new Dictionary<string, string>
        {
            ["Components/ErrorDisplay.razor"] = """
            @inherits ErrorModelComponent

            <div class="error">
                <p>Error: @Model.Message</p>
            </div>
            """
            // Note: No other razor file uses <ErrorDisplay /> in this assembly
            // This simulates the RxBlazorVSSampleComponents/ErrorManager scenario
        };

        // Expected: NO diagnostic because component is not used in same assembly
        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["ErrorDisplay.g.cs"] = GenerateFilterCodeBehind(
                "ErrorDisplay",
                "ErrorModelComponent",
                "Components",
                new[] { "Model.Message" }, new[] { "Test" })
        };

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "ErrorModel",
            "ErrorModelComponent",
            additionalGeneratedSources: additionalGeneratedSources);
            // No expected diagnostic - this is the key difference!
    }

}
