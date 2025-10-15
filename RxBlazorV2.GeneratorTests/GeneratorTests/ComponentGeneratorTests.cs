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
        using RxBlazorV2.Attributes;

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

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged(nameof(Counter));
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
        using Test;

        namespace Test.Components;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
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

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }
    
    [Fact]
    public async Task ComponentWithCustomNamingOnly()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent("TestModelCustomNamedComponent")]
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

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged(nameof(Counter));
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
        using Test;

        namespace Test.Components;

        public partial class TestModelCustomNamedComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
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

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(test, generatedModel, generatedComponent, "TestModel", "TestModelCustomNamedComponent");
    }
    
    [Fact]
    public async Task ComponentTrigger_SyncOnly_GeneratesSyncHookOnly()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent("TestModelComponent")]
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

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged(nameof(Counter));
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
        using Test;

        namespace Test.Components;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
                
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .Subscribe(_ =>
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
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent("TestModelComponent")]
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

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged(nameof(Counter));
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
        using Test;

        namespace Test.Components;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
                
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .SubscribeAwait(async (_,ct) =>
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
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent("TestModelComponent")]
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

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged(nameof(Counter));
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
        using Test;

        namespace Test.Components;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
                
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .Subscribe(_ =>
                    {
                        OnCounterChanged();
                    }));
                
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .SubscribeAwait(async (_,ct) =>
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
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent("TestModelComponent")]
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

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged(nameof(Counter));
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
        using Test;

        namespace Test.Components;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
                
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .Subscribe(_ =>
                    {
                        HandleCounterUpdate();
                    }));
                
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .SubscribeAwait(async (_,ct) =>
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
        using RxBlazorV2.Attributes;

        namespace Test
        {
            [ObservableComponent("TestModelComponent")]
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

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged(nameof(Counter));
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
                        StateHasChanged(nameof(Name));
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
                        StateHasChanged(nameof(IsActive));
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
        using Test;

        namespace Test.Components;

        public partial class TestModelComponent : ObservableComponent<TestModel>
        {
            protected override void InitializeGeneratedCode()
            {
                // Subscribe to model changes for component base model and other models from properties
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter", "Name", "IsActive"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        InvokeAsync(StateHasChanged);
                    }));
                
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Counter"]).Any())
                    .Subscribe(_ =>
                    {
                        OnCounterChanged();
                    }));
                
                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Name"]).Any())
                    .SubscribeAwait(async (_,ct) =>
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
