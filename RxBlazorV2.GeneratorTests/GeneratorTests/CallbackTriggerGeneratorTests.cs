using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for ObservableCallbackTrigger code generation to ensure correct callback registration methods.
/// </summary>
public class CallbackTriggerGeneratorTests
{
    [Fact]
    public async Task SyncCallbackTrigger_GeneratesCallbackRegistrationMethod()
    {
        // lang=csharp
        const string test = """
            using RxBlazorV2.Model;

            namespace Test
            {
                public partial class TestModel : ObservableModel
                {
                    [ObservableCallbackTrigger]
                    public partial string CurrentUser { get; set; } = "";
                }
            }
            """;

        // lang=csharp
        const string expected = """
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

                public partial string CurrentUser
                {
                    get => field;
                    [UsedImplicitly]
                    set
                    {
                        if (field != value)
                        {
                            field = value;
                            StateHasChanged("Model.CurrentUser");
                        }
                    }
                }

                // Callback storage for external subscriptions
                private readonly List<Action> _onCurrentUserChangedCallbacks = new();

                // Callback registration methods for external subscriptions

                /// <summary>
                /// Registers a callback to be invoked when the CurrentUser property changes.
                /// Subscriptions are automatically disposed when the model is disposed.
                /// </summary>
                /// <param name="callback">The callback to invoke on property changes.</param>
                public void OnCurrentUserChanged(Action callback)
                {
                    if (callback is null)
                    {
                        throw new ArgumentNullException(nameof(callback));
                    }

                    _onCurrentUserChangedCallbacks.Add(callback);
                }


                public TestModel() : base()
                {
                    // Subscribe callback triggers for external subscriptions

                    // Sync callbacks for CurrentUser
                    Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CurrentUser"]).Any())
                        .Subscribe(_ =>
                        {
                            foreach (var callback in _onCurrentUserChangedCallbacks)
                            {
                                callback();
                            }
                        }));
                }
            }

            """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "TestModel", string.Empty);
    }

    [Fact]
    public async Task AsyncCallbackTrigger_GeneratesAsyncCallbackRegistrationMethod()
    {
        // lang=csharp
        const string test = """
            using RxBlazorV2.Model;

            namespace Test
            {
                public partial class TestModel : ObservableModel
                {
                    [ObservableCallbackTriggerAsync]
                    public partial string Settings { get; set; } = "";
                }
            }
            """;

        // lang=csharp
        const string expected = """
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

                public partial string Settings
                {
                    get => field;
                    [UsedImplicitly]
                    set
                    {
                        if (field != value)
                        {
                            field = value;
                            StateHasChanged("Model.Settings");
                        }
                    }
                }

                // Callback storage for external subscriptions
                private readonly List<Func<CancellationToken, Task>> _onSettingsChangedAsyncCallbacks = new();

                // Callback registration methods for external subscriptions

                /// <summary>
                /// Registers a callback to be invoked when the Settings property changes.
                /// The callback receives a CancellationToken for async operations.
                /// Subscriptions are automatically disposed when the model is disposed.
                /// </summary>
                /// <param name="callback">The callback to invoke on property changes.</param>
                public void OnSettingsChangedAsync(Func<CancellationToken, Task> callback)
                {
                    if (callback is null)
                    {
                        throw new ArgumentNullException(nameof(callback));
                    }

                    _onSettingsChangedAsyncCallbacks.Add(callback);
                }


                public TestModel() : base()
                {
                    // Subscribe callback triggers for external subscriptions

                    // Async callbacks for Settings
                    Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Settings"]).Any())
                        .SubscribeAwait(async (_, ct) =>
                        {
                            foreach (var callback in _onSettingsChangedAsyncCallbacks)
                            {
                                await callback(ct);
                            }
                        }, AwaitOperation.Switch));
                }
            }

            """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "TestModel", string.Empty);
    }

    [Fact]
    public async Task CallbackTriggerWithCustomName_GeneratesCustomMethodName()
    {
        // lang=csharp
        const string test = """
            using RxBlazorV2.Model;

            namespace Test
            {
                public partial class TestModel : ObservableModel
                {
                    [ObservableCallbackTrigger("HandleThemeUpdate")]
                    public partial string Theme { get; set; } = "";
                }
            }
            """;

        // lang=csharp
        const string expected = """
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

                // Callback storage for external subscriptions
                private readonly List<Action> _handleThemeUpdateCallbacks = new();

                // Callback registration methods for external subscriptions

                /// <summary>
                /// Registers a callback to be invoked when the Theme property changes.
                /// Subscriptions are automatically disposed when the model is disposed.
                /// </summary>
                /// <param name="callback">The callback to invoke on property changes.</param>
                public void HandleThemeUpdate(Action callback)
                {
                    if (callback is null)
                    {
                        throw new ArgumentNullException(nameof(callback));
                    }

                    _handleThemeUpdateCallbacks.Add(callback);
                }


                public TestModel() : base()
                {
                    // Subscribe callback triggers for external subscriptions

                    // Sync callbacks for Theme
                    Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Theme"]).Any())
                        .Subscribe(_ =>
                        {
                            foreach (var callback in _handleThemeUpdateCallbacks)
                            {
                                callback();
                            }
                        }));
                }
            }

            """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "TestModel", string.Empty);
    }

    [Fact]
    public async Task BothSyncAndAsyncCallbackTriggers_GeneratesBothMethods()
    {
        // lang=csharp
        const string test = """
            using RxBlazorV2.Model;

            namespace Test
            {
                public partial class TestModel : ObservableModel
                {
                    [ObservableCallbackTrigger]
                    [ObservableCallbackTriggerAsync]
                    public partial int Count { get; set; }
                }
            }
            """;

        // lang=csharp
        const string expected = """
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

                // Callback storage for external subscriptions
                private readonly List<Action> _onCountChangedCallbacks = new();
                private readonly List<Func<CancellationToken, Task>> _onCountChangedAsyncCallbacks = new();

                // Callback registration methods for external subscriptions

                /// <summary>
                /// Registers a callback to be invoked when the Count property changes.
                /// Subscriptions are automatically disposed when the model is disposed.
                /// </summary>
                /// <param name="callback">The callback to invoke on property changes.</param>
                public void OnCountChanged(Action callback)
                {
                    if (callback is null)
                    {
                        throw new ArgumentNullException(nameof(callback));
                    }

                    _onCountChangedCallbacks.Add(callback);
                }

                /// <summary>
                /// Registers a callback to be invoked when the Count property changes.
                /// The callback receives a CancellationToken for async operations.
                /// Subscriptions are automatically disposed when the model is disposed.
                /// </summary>
                /// <param name="callback">The callback to invoke on property changes.</param>
                public void OnCountChangedAsync(Func<CancellationToken, Task> callback)
                {
                    if (callback is null)
                    {
                        throw new ArgumentNullException(nameof(callback));
                    }

                    _onCountChangedAsyncCallbacks.Add(callback);
                }


                public TestModel() : base()
                {
                    // Subscribe callback triggers for external subscriptions

                    // Sync callbacks for Count
                    Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Count"]).Any())
                        .Subscribe(_ =>
                        {
                            foreach (var callback in _onCountChangedCallbacks)
                            {
                                callback();
                            }
                        }));

                    // Async callbacks for Count
                    Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Count"]).Any())
                        .SubscribeAwait(async (_, ct) =>
                        {
                            foreach (var callback in _onCountChangedAsyncCallbacks)
                            {
                                await callback(ct);
                            }
                        }, AwaitOperation.Switch));
                }
            }

            """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "TestModel", string.Empty);
    }
}
