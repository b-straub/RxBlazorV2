using Microsoft.CodeAnalysis.Testing;
using RxBlazorV2.GeneratorTests.Helpers;
using RxBlazorV2Generator.Diagnostics;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for Razor file-based diagnostics (RXBG060, RXBG014)
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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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
        using Test;

        namespace Test.Components;

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

        // Razor file that inherits from generated component - no error expected
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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
        using Test;

        namespace Test.Components;

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
            {|#0:@inherits TestModelComponent|}

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Counter.razor"] = """
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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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
        using Test;

        namespace Test.Components;

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
            {|#0:@inherits TestModelComponent|}

            <h3>Page 1</h3>
            <p>Count: @Model.Count</p>
            """,
            ["Pages/Page2.razor"] = """
            {|#1:@inherits TestModelComponent|}

            <h3>Page 2</h3>
            <p>Count: @Model.Count</p>
            """,
            ["Pages/Page3.razor"] = """
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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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
        using Test;

        namespace Test.Components;

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
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Counter.razor"] = """
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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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
        using Test;

        namespace Test.Components;

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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

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
        using Test;

        namespace Test.Components;

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
            {|#0:@inherits TestModelComponent|}

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Weather.razor.cs"] = """
            using Test.Components;

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
            {|#1:@inherits TestModelComponent|}

            <h3>Counter</h3>
            <p>@Model.Name</p>
            """,
            ["Pages/Counter.razor.cs"] = """
            using Test.Components;

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
}
