using Microsoft.CodeAnalysis.Testing;
using RxBlazorV2.GeneratorTests.Helpers;
using RxBlazorV2Generator.Diagnostics;
using System.Text;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for RXBG042: Redundant ObservableComponentTrigger on razor-observed property.
/// A trigger with RenderOnly or RenderAndHook behavior is an error when the property
/// is already referenced in the razor file. HookOnly is valid.
/// </summary>
public class RedundantComponentTriggerTests
{
    private static string GenerateFilterCodeBehind(string componentName, string baseClass, string namespaceName, string[] filterProperties, string[]? usingDirectives = null)
    {
        var sb = new StringBuilder();

        if (usingDirectives is not null && usingDirectives.Length > 0)
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

        if (filterProperties.Length > 0)
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
            sb.AppendLine("        // No properties detected in razor file - no automatic StateHasChanged");
            sb.AppendLine("        return [];");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // Shared model generation for a model with a single string property "Name"
    private const string GeneratedModelWithName = """

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

        public override bool FilterUsedProperties(params string[] propertyNames)
        {
            if (propertyNames.Length == 0)
            {
                return false;
            }

            // No filtering information available - pass through all
            return true;
        }

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

    // Component with sync RenderAndHook trigger on Name
    private const string GeneratedComponentRenderAndHook = """

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
            if (filter.Length > 0)
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
            // else: Empty filter - no automatic StateHasChanged, only triggers (if any) will fire

            Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Name"]).Any())
                .Chunk(TimeSpan.FromMilliseconds(100))
                .Subscribe(chunks =>
                {
                    OnNameChanged();
                }));
        }

        protected override Task InitializeGeneratedCodeAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnNameChanged()
        {
        }

        protected sealed override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        protected sealed override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }
    }

    """;

    // Component with RenderOnly trigger on Name (no hook, no subscription)
    private const string GeneratedComponentRenderOnly = """

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
            if (filter.Length > 0)
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
            // else: Empty filter - no automatic StateHasChanged, only triggers (if any) will fire
        }

        protected override Task InitializeGeneratedCodeAsync()
        {
            return Task.CompletedTask;
        }

        protected sealed override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        protected sealed override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }
    }

    """;

    // Component with HookOnly trigger on Name
    private const string GeneratedComponentHookOnly = """

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
            if (filter.Length > 0)
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
            // else: Empty filter - no automatic StateHasChanged, only triggers (if any) will fire

            Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Name"]).Any())
                .Chunk(TimeSpan.FromMilliseconds(100))
                .Subscribe(chunks =>
                {
                    OnNameChanged();
                }));
        }

        protected override Task InitializeGeneratedCodeAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnNameChanged()
        {
        }

        protected sealed override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        protected sealed override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }
    }

    """;

    [Fact]
    public async Task RenderAndHookTrigger_PropertyInRazor_ReportsRXBG042()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }
            }
        }
        """;

        // Razor file references Model.Name - trigger is redundant
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """
        };

        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                ["Model.Name"], ["Test"])
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.RedundantComponentTriggerError)
            .WithSpan(11, 10, 11, 36)
            .WithArguments("Name", "TestModel", "RenderAndHook", "Weather");

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            GeneratedModelWithName,
            GeneratedComponentRenderAndHook,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task RenderOnlyTrigger_PropertyInRazor_ReportsRXBG042()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger(type: ComponentTriggerType.RenderOnly)]
                public partial string Name { get; set; }
            }
        }
        """;

        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """
        };

        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                ["Model.Name"], ["Test"])
        };

        var expected = new DiagnosticResult(DiagnosticDescriptors.RedundantComponentTriggerError)
            .WithSpan(11, 10, 11, 75)
            .WithArguments("Name", "TestModel", "RenderOnly", "Weather");

        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            GeneratedModelWithName,
            GeneratedComponentRenderOnly,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
            additionalGeneratedSources: additionalGeneratedSources,
            expected: expected);
    }

    [Fact]
    public async Task HookOnlyTrigger_PropertyInRazor_NoDiagnostic()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger(type: ComponentTriggerType.HookOnly)]
                public partial string Name { get; set; }
            }
        }
        """;

        // Razor file references Model.Name but trigger is HookOnly - no diagnostic
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Name</p>
            """
        };

        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                ["Model.Name"], ["Test"])
        };

        // No expected diagnostic - HookOnly is valid even when property is in razor
        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            GeneratedModelWithName,
            GeneratedComponentHookOnly,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
            additionalGeneratedSources: additionalGeneratedSources);
    }

    [Fact]
    public async Task TriggerProperty_NotInRazor_NoDiagnostic()
    {
        // lang=csharp
        const string source = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent]
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial string Name { get; set; }

                public partial int Counter { get; set; }
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

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

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

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Counter");
                    }
                }
            }

        }

        """;

        // Razor file only uses Counter, not Name - trigger on Name is NOT redundant
        var razorFiles = new Dictionary<string, string>
        {
            ["Pages/Weather.razor"] = """
            @page "/weather"
            @inherits TestModelComponent

            <h3>Weather</h3>
            <p>@Model.Counter</p>
            """
        };

        var additionalGeneratedSources = new Dictionary<string, string>
        {
            ["Weather.g.cs"] = GenerateFilterCodeBehind(
                "Weather",
                "TestModelComponent",
                "Pages",
                ["Model.Counter", "Model.Name"], ["Test"])
        };

        // No expected diagnostic - Name is not in razor
        await RazorFileGeneratorVerifier.VerifyRazorDiagnosticsAsync(
            source,
            razorFiles,
            generatedModel,
            GeneratedComponentRenderAndHook,
            "TestModel",
            "TestModelComponent",
            modelScope: "Singleton",
            additionalGeneratedSources: additionalGeneratedSources);
    }
}
