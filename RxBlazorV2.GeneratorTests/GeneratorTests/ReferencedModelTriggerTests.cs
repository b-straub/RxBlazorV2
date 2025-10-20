using Microsoft.CodeAnalysis.Testing;
using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for RXBG052: Referenced model with triggers must be in same assembly
/// and includeReferencedTriggers feature of [ObservableComponent] attribute.
///
/// Note: These tests focus on same-assembly scenarios and feature flag behavior.
/// Testing actual cross-assembly errors (RXBG052) requires integration tests with
/// multiple project references, which is beyond the scope of unit tests.
///
/// The RXBG052 diagnostic is reported when:
/// - A model has [ObservableComponent(includeReferencedTriggers: true)] (default)
/// - The model references another ObservableModel from a DIFFERENT assembly
/// - The referenced model has [ObservableComponentTrigger] attributes
///
/// These tests verify the includeReferencedTriggers feature works correctly
/// when models are in the SAME assembly (no RXBG052 error).
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
        using RxBlazorV2.Attributes;

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
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class SettingsModel
        {
            public override string ModelID => "Test.SettingsModel";

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
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;
        using System.Linq;

        namespace Test;

        public partial class WeatherModel
        {
            public override string ModelID => "Test.WeatherModel";

            public Test.SettingsModel Settings { get; }



            public partial WeatherModel(Test.SettingsModel settings) : base()
            {
                Settings = settings;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                Subscriptions.Add(Settings.Observable
                    .Select(props => props.Intersect([""]).Select(p => p.Replace("Model.", "Model.Settings.")).ToArray())
                    .Where(transformed => transformed.Length > 0)
                    .Subscribe(props => StateHasChanged(props)));
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

            public Test.SettingsModel Settings { get; }



            public partial WeatherModel(Test.SettingsModel settings) : base()
            {
                Settings = settings;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                Subscriptions.Add(Settings.Observable
                    .Select(props => props.Intersect([""]).Select(p => p.Replace("Model.", "Model.Settings.")).ToArray())
                    .Where(transformed => transformed.Length > 0)
                    .Subscribe(props => StateHasChanged(props)));
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

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test,
            [generatedSettingsModel, generatedWeatherModel],
            generatedComponent,
            ["SettingsModel", "WeatherModel"],
            "WeatherModelComponent",
            "");
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
        using RxBlazorV2.Attributes;

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
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class SettingsModel
        {
            public override string ModelID => "Test.SettingsModel";

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
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;
        using System.Linq;

        namespace Test;

        public partial class WeatherModel
        {
            public override string ModelID => "Test.WeatherModel";

            public Test.SettingsModel Settings { get; }



            public partial WeatherModel(Test.SettingsModel settings) : base()
            {
                Settings = settings;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                Subscriptions.Add(Settings.Observable
                    .Select(props => props.Intersect([""]).Select(p => p.Replace("Model.", "Model.Settings.")).ToArray())
                    .Where(transformed => transformed.Length > 0)
                    .Subscribe(props => StateHasChanged(props)));
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
        using RxBlazorV2.Attributes;

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
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class SettingsModel
        {
            public override string ModelID => "Test.SettingsModel";

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
        using RxBlazorV2.Attributes;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;
        using System.Linq;

        namespace Test;

        public partial class WeatherModel
        {
            public override string ModelID => "Test.WeatherModel";

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
                Subscriptions.Add(Settings.Observable
                    .Select(props => props.Intersect([""]).Select(p => p.Replace("Model.", "Model.Settings.")).ToArray())
                    .Where(transformed => transformed.Length > 0)
                    .Subscribe(props => StateHasChanged(props)));
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
        using RxBlazorV2.Attributes;

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

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test,
            generatedModel,
            generatedComponent,
            "TestModel",
            "TestModelComponent");
    }
}
