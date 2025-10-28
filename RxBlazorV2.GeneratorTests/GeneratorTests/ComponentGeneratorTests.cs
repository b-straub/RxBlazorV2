using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for ObservableComponent, ObservableComponentTrigger and ObservableComponentTriggerAsync attributes
/// to ensure only the needed hook methods are generated.
/// </summary>
public class ComponentTriggerGeneratorTests
{
    [Fact]
    public async Task ComponentOnly()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
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

        // Now verify both model and component generation
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

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }
    
    [Fact]
    public async Task ComponentWithCustomNamingOnly()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent(true, "TestModelCustomNamedComponent")]
            public partial class TestModel : ObservableModel
            {
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

        // Now verify both model and component generation
        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class TestModelCustomNamedComponent : ObservableComponent<TestModel>
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

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelCustomNamedComponent");
    }
    
    [Fact]
    public async Task ComponentTrigger_SyncOnly_GeneratesSyncHookOnly()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent(true, "TestModelComponent")]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
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

        // Now verify both model and component generation
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnCounterChanged();
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnCounterChanged()
            {
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }

    [Fact]
    public async Task ComponentTrigger_AsyncOnly_GeneratesAsyncHookOnly()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent(true, "TestModelComponent")]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTriggerAsync]
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

        // Now verify both model and component generation
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .SubscribeAwait(async (chunks, ct) =>
                    {
                        await OnCounterChangedAsync(ct);
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual Task OnCounterChangedAsync(CancellationToken ct)
            {
                return Task.CompletedTask;
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }

    [Fact]
    public async Task ComponentTrigger_BothAttributes_GeneratesBothHooks()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent(true, "TestModelComponent")]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                [ObservableComponentTriggerAsync]
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

        // Now verify the component generation - should have both hooks
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnCounterChanged();
                    }));

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .SubscribeAwait(async (chunks, ct) =>
                    {
                        await OnCounterChangedAsync(ct);
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnCounterChanged()
            {
            }

            protected virtual Task OnCounterChangedAsync(CancellationToken ct)
            {
                return Task.CompletedTask;
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }

    [Fact]
    public async Task ComponentTrigger_CustomHookNames_UsesCustomNames()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent(true, "TestModelComponent")]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger("HandleCounterUpdate")]
                [ObservableComponentTriggerAsync("HandleCounterUpdateAsync")]
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

        // Now verify the component generation with custom hook names
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        HandleCounterUpdate();
                    }));

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .SubscribeAwait(async (chunks, ct) =>
                    {
                        await HandleCounterUpdateAsync(ct);
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void HandleCounterUpdate()
            {
            }

            protected virtual Task HandleCounterUpdateAsync(CancellationToken ct)
            {
                return Task.CompletedTask;
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }

    [Fact]
    public async Task ComponentTrigger_MultipleProperties_GeneratesCorrectHooks()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent(true, "TestModelComponent")]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public partial int Counter { get; set; }

                [ObservableComponentTriggerAsync]
                public partial string Name { get; set; }

                public partial bool IsActive { get; set; }
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

            public partial bool IsActive
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.IsActive");
                    }
                }
            }

        }

        """;

        // Now verify the component generation
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnCounterChanged();
                    }));

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Name"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .SubscribeAwait(async (chunks, ct) =>
                    {
                        await OnNameChangedAsync(ct);
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnCounterChanged()
            {
            }

            protected virtual Task OnNameChangedAsync(CancellationToken ct)
            {
                return Task.CompletedTask;
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }
}
