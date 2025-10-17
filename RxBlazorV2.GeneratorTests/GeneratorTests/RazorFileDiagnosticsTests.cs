using Microsoft.CodeAnalysis.Testing;
using RxBlazorV2.GeneratorTests.Helpers;
using RxBlazorV2Generator.Diagnostics;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for Razor file-based diagnostics (RXBG060, RXBG061, RXBG014)
/// These tests verify diagnostics that are reported based on razor file content
/// </summary>
public class RazorFileDiagnosticsTests
{
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
                        StateHasChanged(nameof(Name));
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
                        StateHasChanged(nameof(Name));
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
                        StateHasChanged(nameof(Name));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent");
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
                        StateHasChanged(nameof(Name));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Scoped",
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
                        StateHasChanged(nameof(Count));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Count"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Transient",
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
                        StateHasChanged(nameof(Name));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton");
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
                        StateHasChanged(nameof(Name));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Scoped");
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
                        StateHasChanged(nameof(Name));
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
                        StateHasChanged(nameof(Name));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Scoped",
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
                        StateHasChanged(nameof(Name));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent");
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
                        StateHasChanged(nameof(Name));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Razor file without @page directive - SHOULD report RXBG061
        var razorFiles = new Dictionary<string, string>
        {
            ["Components/Widget.razor"] = """
            {|#0:@inherits TestModelComponent|}

            <div class="widget">
                <h3>@Model.Name</h3>
            </div>
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
            .WithLocation(0)
            .WithArguments("Widget.razor", "TestModelComponent");

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
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
                        StateHasChanged(nameof(Name));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Multiple razor files without @page directive - all should report RXBG061
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
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
                        StateHasChanged(nameof(Title));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Title"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
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
            """,
            // Page - should NOT report error
            ["Pages/Settings.razor"] = """
            @page "/settings"
            @inherits TestModelComponent

            <h1>Settings</h1>
            <p>@Model.Title</p>
            """,
            // Component - SHOULD report error
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

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
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
                        StateHasChanged(nameof(Data));
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
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Data"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

        }

        """;

        // Razor file without @page using custom-named component - SHOULD report RXBG061
        var razorFiles = new Dictionary<string, string>
        {
            ["Components/MyWidget.razor"] = """
            {|#0:@inherits CustomComponent|}

            <div>
                <p>@Model.Data</p>
            </div>
            """
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.SameAssemblyComponentCompositionError)
            .WithLocation(0)
            .WithArguments("MyWidget.razor", "CustomComponent");

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            generatedComponent,
            "TestModel",
            "CustomComponent",
            expected: expected);
    }
}
