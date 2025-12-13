using Microsoft.CodeAnalysis.Testing;
using RxBlazorV2.GeneratorTests.Helpers;
using RxBlazorV2Generator.Diagnostics;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for includeReferencedTriggers feature of [ObservableComponent] attribute.
///
/// The generator now supports cross-assembly trigger references by using
/// Compilation.GetTypeByMetadataName to read trigger attributes from referenced
/// models in different assemblies. This means trigger hooks can be generated
/// for both same-assembly and cross-assembly model references.
///
/// These tests verify the includeReferencedTriggers feature works correctly
/// for same-assembly scenarios. Cross-assembly scenarios work the same way
/// but would require integration tests with multiple project references.
/// </summary>
public class ReferencedModelTriggerTests
{
    [Fact]
    public async Task IncludeReferencedTriggersDefault_GeneratesHookForReferencedModelTrigger()
    {
        // This test verifies that when includeReferencedTriggers is true (default),
        // hooks are generated for triggers on referenced models in the same assembly.

        // We test this by having a WeatherModel reference a SettingsModel,
        // where SettingsModel has a trigger on the IsDay property.
        // The generated WeatherModelComponent should have a hook for Settings.IsDay changes.

        // For simplicity, we only test the WeatherModel component generation
        // since the SettingsModel component is tested elsewhere.

        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class SettingsModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial bool IsDay { get; set; }
            }

            [ObservableComponent]  // includeReferencedTriggers defaults to true
            public partial class WeatherModel : ObservableModel
            {
                public partial WeatherModel(SettingsModel settings);
            }
        }
        """;

        // Generated SettingsModel
        // lang=csharp
        const string generatedSettingsModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class SettingsModel
        {
            public override string ModelID => "Test.SettingsModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial bool IsDay
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.IsDay");
                    }
                }
            }

        }

        """;

        // Generated model should have Settings property and subscription
        // lang=csharp
        const string generatedWeatherModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;
        using System.Linq;

        namespace Test;

        public partial class WeatherModel
        {
            public override string ModelID => "Test.WeatherModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public Test.SettingsModel Settings { get; }



            public partial WeatherModel(Test.SettingsModel settings) : base()
            {
                Settings = settings;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Settings.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Settings.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;

                // Initialize referenced ObservableModel dependencies
                Settings.ContextReady();

            }

            private bool _contextReadyInternAsyncCalled;

            protected override async Task OnContextReadyInternAsync()
            {
                if (_contextReadyInternAsyncCalled)
                {
                    return;
                }
                _contextReadyInternAsyncCalled = true;

                // Initialize referenced ObservableModel dependencies (async)
                await Settings.ContextReadyAsync();
            }

        }

        """;

        // Generated component should have hook for Settings.IsDay
        // Hook naming: On{ReferencedProperty}{TriggerProperty}Changed
        // -> OnSettingsIsDayChanged
        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class WeatherModelComponent : ObservableComponent<WeatherModel>
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Settings.IsDay"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnSettingsIsDayChanged();
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnSettingsIsDayChanged()
            {
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test,
            [generatedSettingsModel, generatedWeatherModel],
            generatedComponent,
            ["SettingsModel", "WeatherModel"],
            "WeatherModelComponent");
    }

    [Fact]
    public async Task IncludeReferencedTriggersFalse_DoesNotGenerateHooksForReferencedModel()
    {
        // This test verifies that when includeReferencedTriggers is false,
        // NO hooks are generated for triggers on referenced models.
        // Additionally, since the trigger is not used, RXBG041 should be reported.

        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class SettingsModel : ObservableModel
            {
                [{|#0:ObservableComponentTrigger|}]
                public partial bool IsDay { get; set; }
            }

            [ObservableComponent(includeReferencedTriggers: false)]  // Explicitly disabled
            public partial class WeatherModel : ObservableModel
            {
                public partial WeatherModel(SettingsModel settings);
            }
        }
        """;

        // Generated SettingsModel
        // lang=csharp
        const string generatedSettingsModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class SettingsModel
        {
            public override string ModelID => "Test.SettingsModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial bool IsDay
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.IsDay");
                    }
                }
            }

        }

        """;

        // Generated model (same as before)
        // lang=csharp
        const string generatedWeatherModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;
        using System.Linq;

        namespace Test;

        public partial class WeatherModel
        {
            public override string ModelID => "Test.WeatherModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public Test.SettingsModel Settings { get; }



            public partial WeatherModel(Test.SettingsModel settings) : base()
            {
                Settings = settings;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Settings.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Settings.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;

                // Initialize referenced ObservableModel dependencies
                Settings.ContextReady();

            }

            private bool _contextReadyInternAsyncCalled;

            protected override async Task OnContextReadyInternAsync()
            {
                if (_contextReadyInternAsyncCalled)
                {
                    return;
                }
                _contextReadyInternAsyncCalled = true;

                // Initialize referenced ObservableModel dependencies (async)
                await Settings.ContextReadyAsync();
            }

        }

        """;

        // Generated component should NOT have hook for Settings.IsDay
        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class WeatherModelComponent : ObservableComponent<WeatherModel>
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

        }

        """;

        var expected = DiagnosticResult
            .CompilerWarning(DiagnosticDescriptors.UnusedObservableComponentTriggerWarning.Id)
            .WithSpan(9, 10, 9, 36)
            .WithArguments("IsDay", "SettingsModel");

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test,
            [generatedSettingsModel, generatedWeatherModel],
            generatedComponent,
            ["SettingsModel", "WeatherModel"],
            "WeatherModelComponent",
            "",
            expected);
    }

    [Fact]
    public async Task MultipleReferencedTriggers_GeneratesBothSyncAndAsyncHooks()
    {
        // Test that when a referenced model has both sync and async triggers,
        // both types of hooks are generated.

        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class SettingsModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial bool IsDay { get; set; }

                [ObservableComponentTriggerAsync]
                public partial string Theme { get; set; }
            }

            [ObservableComponent]
            public partial class WeatherModel : ObservableModel
            {
                public partial WeatherModel(SettingsModel settings);
            }
        }
        """;

        // Generated SettingsModel
        // lang=csharp
        const string generatedSettingsModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class SettingsModel
        {
            public override string ModelID => "Test.SettingsModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial bool IsDay
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.IsDay");
                    }
                }
            }

            public partial string Theme
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Theme");
                    }
                }
            }

        }

        """;

        // lang=csharp
        const string generatedWeatherModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;
        using System.Linq;

        namespace Test;

        public partial class WeatherModel
        {
            public override string ModelID => "Test.WeatherModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public Test.SettingsModel Settings { get; }



            public partial WeatherModel(Test.SettingsModel settings) : base()
            {
                Settings = settings;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Settings.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Settings.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;

                // Initialize referenced ObservableModel dependencies
                Settings.ContextReady();

            }

            private bool _contextReadyInternAsyncCalled;

            protected override async Task OnContextReadyInternAsync()
            {
                if (_contextReadyInternAsyncCalled)
                {
                    return;
                }
                _contextReadyInternAsyncCalled = true;

                // Initialize referenced ObservableModel dependencies (async)
                await Settings.ContextReadyAsync();
            }

        }

        """;

        // Component should have both sync and async hooks
        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class WeatherModelComponent : ObservableComponent<WeatherModel>
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Settings.IsDay"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnSettingsIsDayChanged();
                    }));

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Settings.Theme"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .SubscribeAwait(async (chunks, ct) =>
                    {
                        await OnSettingsThemeChangedAsync(ct);
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnSettingsIsDayChanged()
            {
            }

            protected virtual Task OnSettingsThemeChangedAsync(CancellationToken ct)
            {
                return Task.CompletedTask;
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test,
            [generatedSettingsModel, generatedWeatherModel],
            generatedComponent,
            ["SettingsModel", "WeatherModel"],
            "WeatherModelComponent");
    }

    [Fact]
    public async Task IncludeReferencedTriggersTrue_PreventRXBG012Error()
    {
        // This test verifies that when includeReferencedTriggers is true (default),
        // a reference that ONLY has triggers (no code usage) does NOT trigger RXBG012.
        // The reference counts as USED because triggers will generate component hooks.

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class SettingsModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial bool IsDay { get; set; }
            }

            [ObservableComponent]  // includeReferencedTriggers defaults to true
            public partial class WeatherModel : ObservableModel
            {
                // NO RXBG012 error: settings reference counts as USED via triggers
                public partial WeatherModel(SettingsModel settings);

                public partial string Temperature { get; set; }
            }
        }
        """;

        // Generated SettingsModel
        // lang=csharp
        const string generatedSettingsModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class SettingsModel
        {
            public override string ModelID => "Test.SettingsModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial bool IsDay
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.IsDay");
                    }
                }
            }

        }

        """;

        // Generated WeatherModel
        // lang=csharp
        const string generatedWeatherModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;
        using System.Linq;

        namespace Test;

        public partial class WeatherModel
        {
            public override string ModelID => "Test.WeatherModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public Test.SettingsModel Settings { get; }

            public partial string Temperature
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Temperature");
                    }
                }
            }


            public partial WeatherModel(Test.SettingsModel settings) : base()
            {
                Settings = settings;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Settings.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Settings.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;

                // Initialize referenced ObservableModel dependencies
                Settings.ContextReady();

            }

            private bool _contextReadyInternAsyncCalled;

            protected override async Task OnContextReadyInternAsync()
            {
                if (_contextReadyInternAsyncCalled)
                {
                    return;
                }
                _contextReadyInternAsyncCalled = true;

                // Initialize referenced ObservableModel dependencies (async)
                await Settings.ContextReadyAsync();
            }

        }

        """;

        // Generated component with hook
        // lang=csharp
        const string generatedComponent = """
        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class WeatherModelComponent : ObservableComponent<WeatherModel>
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Settings.IsDay"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnSettingsIsDayChanged();
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnSettingsIsDayChanged()
            {
            }
        }

        """;

        // NO diagnostic expected - reference is used via triggers
        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test,
            [generatedSettingsModel, generatedWeatherModel],
            generatedComponent,
            ["SettingsModel", "WeatherModel"],
            "WeatherModelComponent");
    }

    [Fact]
    public async Task NoReferencedModels_NoHooksGenerated()
    {
        // Baseline test: model with no references should not have any cross-model hooks

        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
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

        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent");
    }
}
