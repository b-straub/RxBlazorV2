using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for [ObservableComponentBatchAsync]: one Switch-dispatched hook per batch, a single
/// filtered stream when every member shares a debounce window, and a merged stream when they do not.
/// Also guards that [ObservableBatch] is untouched by any of it.
/// </summary>
public class ComponentBatchGeneratorTests
{
    /// <summary>
    /// The model half is identical across these cases: [ObservableComponentBatchAsync] never reaches
    /// StateHasChanged, so the generated properties carry no batch id.
    /// </summary>
    private const string PlainModel = """

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

            public partial string SearchTerm
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.SearchTerm");
                    }
                }
            }

            public partial bool HighlightMatches
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.HighlightMatches");
                    }
                }
            }

        }

        """;

    private const string ComponentHeader = """

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

        """;

    private const string ComponentFooter = """

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual Task OnSearchBatchChangedAsync(CancellationToken ct)
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

    [Fact]
    public async Task UniformDebounceWindow_GeneratesSingleFilteredStream()
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
                [ObservableComponentBatchAsync("search", 250)]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("search", 250)]
                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        const string subscription = """

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.SearchTerm", "Model.HighlightMatches"]).Any())
                    .Debounce(TimeSpan.FromMilliseconds(250))
                    .SubscribeAwait(async (props, ct) =>
                    {
                        await OnSearchBatchChangedAsync(ct);
                    }, AwaitOperation.Switch));
            }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, PlainModel, ComponentHeader + subscription + ComponentFooter, "TestModel", "TestModelComponent");
    }

    [Fact]
    public async Task MixedDebounceWindows_GeneratesMergedStreams()
    {
        // The point of the per-property window: the typed term settles, the switch fires at once.
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableComponentBatchAsync("search", 250)]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("search")]
                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        // Windows are emitted ascending, so the immediate stream comes first.
        const string subscription = """

                Subscriptions.Add(R3.Observable.Merge(
                        Model.Observable.Where(p => p.Intersect(["Model.HighlightMatches"]).Any()),
                        Model.Observable.Where(p => p.Intersect(["Model.SearchTerm"]).Any())
                            .Debounce(TimeSpan.FromMilliseconds(250)))
                    .SubscribeAwait(async (props, ct) =>
                    {
                        await OnSearchBatchChangedAsync(ct);
                    }, AwaitOperation.Switch));
            }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, PlainModel, ComponentHeader + subscription + ComponentFooter, "TestModel", "TestModelComponent");
    }

    [Fact]
    public async Task AllWindowsZero_GeneratesNoDebounceOperator()
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
                [ObservableComponentBatchAsync("search")]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("search")]
                public partial bool HighlightMatches { get; set; }
            }
        }
        """;

        const string subscription = """

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.SearchTerm", "Model.HighlightMatches"]).Any())
                    .SubscribeAwait(async (props, ct) =>
                    {
                        await OnSearchBatchChangedAsync(ct);
                    }, AwaitOperation.Switch));
            }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, PlainModel, ComponentHeader + subscription + ComponentFooter, "TestModel", "TestModelComponent");
    }

    [Fact]
    public async Task TwoBatches_GenerateOneHookEach()
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
                [ObservableComponentBatchAsync("search", 250)]
                public partial string SearchTerm { get; set; }

                [ObservableComponentBatchAsync("paging", 500)]
                public partial int PageSize { get; set; }
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

            public partial string SearchTerm
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.SearchTerm");
                    }
                }
            }

            public partial int PageSize
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.PageSize");
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.SearchTerm"]).Any())
                    .Debounce(TimeSpan.FromMilliseconds(250))
                    .SubscribeAwait(async (props, ct) =>
                    {
                        await OnSearchBatchChangedAsync(ct);
                    }, AwaitOperation.Switch));

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.PageSize"]).Any())
                    .Debounce(TimeSpan.FromMilliseconds(500))
                    .SubscribeAwait(async (props, ct) =>
                    {
                        await OnPagingBatchChangedAsync(ct);
                    }, AwaitOperation.Switch));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual Task OnSearchBatchChangedAsync(CancellationToken ct)
            {
                return Task.CompletedTask;
            }

            protected virtual Task OnPagingBatchChangedAsync(CancellationToken ct)
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

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }

    [Fact]
    public async Task ObservableBatch_IsUnaffected()
    {
        // Regression guard for the revert: [ObservableBatch] still only tags StateHasChanged for
        // SuspendNotifications scopes, and generates no component hook of any kind.
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableComponent]
            public partial class TestModel : ObservableModel
            {
                [ObservableBatch("search")]
                public partial string SearchTerm { get; set; }

                [ObservableBatch("search")]
                public partial bool HighlightMatches { get; set; }
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

            public partial string SearchTerm
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.SearchTerm", "search");
                    }
                }
            }

            public partial bool HighlightMatches
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.HighlightMatches", "search");
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

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, generatedModel, generatedComponent, "TestModel", "TestModelComponent");
    }
}
