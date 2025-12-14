using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for internal and external model observer code generation.
/// Internal observers: Private methods that access injected ObservableModel properties (auto-detected).
/// External observers: Service methods with [ObservableModelObserver] attribute.
/// </summary>
public class ModelObserverGeneratorTests
{
    #region Internal Model Observer Tests

    [Fact]
    public async Task InternalModelObserver_SyncMethod_GeneratesSubscriptionInConstructor()
    {
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial int Counter { get; set; }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                private void OnCounterChanged()
                {
                    System.Console.WriteLine($"Counter: {Referenced.Counter}");
                }
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
        using System.Linq;

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

                var usedProps = new[] { "Model.Referenced.Counter" };

                return propertyNames.Intersect(usedProps).Any();
            }

            public Test.ReferencedModel Referenced { get; }



            public partial TestModel(Test.ReferencedModel referenced) : base()
            {
                Referenced = referenced;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Referenced.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Referenced.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));

                // Subscribe to internal model observers (auto-detected)
                Subscriptions.Add(Referenced.Observable.Where(p => p.Intersect(["Model.Counter"]).Any())
                    .Subscribe(_ => OnCounterChanged()));
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
                Referenced.ContextReady();

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
                await Referenced.ContextReadyAsync();
            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [referencedModel, test],
            [("Test.ReferencedModel.g.cs", GenerateReferencedModelCode()),
             ("Test.TestModel.g.cs", expected)],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_AsyncMethodWithCancellationToken_GeneratesSubscribeAwait()
    {
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial string Name { get; set; } = "";
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;
        using System.Threading.Tasks;
        using System.Threading;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                private async Task OnNameChangedAsync(CancellationToken ct)
                {
                    await Task.Delay(100, ct);
                    System.Console.WriteLine($"Name: {Referenced.Name}");
                }
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
        using System.Linq;
        using System.Threading;
        using System.Threading.Tasks;

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

                var usedProps = new[] { "Model.Referenced.Name" };

                return propertyNames.Intersect(usedProps).Any();
            }

            public Test.ReferencedModel Referenced { get; }



            public partial TestModel(Test.ReferencedModel referenced) : base()
            {
                Referenced = referenced;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Referenced.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Referenced.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));

                // Subscribe to internal model observers (auto-detected)
                Subscriptions.Add(Referenced.Observable.Where(p => p.Intersect(["Model.Name"]).Any())
                    .SubscribeAwait(async (_, ct) => await OnNameChangedAsync(ct), AwaitOperation.Switch));
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
                Referenced.ContextReady();

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
                await Referenced.ContextReadyAsync();
            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [referencedModel, test],
            [("Test.ReferencedModel.g.cs", GenerateReferencedModelCodeWithName()),
             ("Test.TestModel.g.cs", expected)],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_AsyncMethodWithoutCancellationToken_GeneratesSubscribeAwaitWithDiscard()
    {
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial bool IsActive { get; set; }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;
        using System.Threading.Tasks;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                private async Task OnIsActiveChangedAsync()
                {
                    await Task.Delay(50);
                    System.Console.WriteLine($"IsActive: {Referenced.IsActive}");
                }
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
        using System.Linq;
        using System.Threading.Tasks;

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

                var usedProps = new[] { "Model.Referenced.IsActive" };

                return propertyNames.Intersect(usedProps).Any();
            }

            public Test.ReferencedModel Referenced { get; }



            public partial TestModel(Test.ReferencedModel referenced) : base()
            {
                Referenced = referenced;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Referenced.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Referenced.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));

                // Subscribe to internal model observers (auto-detected)
                Subscriptions.Add(Referenced.Observable.Where(p => p.Intersect(["Model.IsActive"]).Any())
                    .SubscribeAwait(async (_, _) => await OnIsActiveChangedAsync(), AwaitOperation.Switch));
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
                Referenced.ContextReady();

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
                await Referenced.ContextReadyAsync();
            }
        
        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [referencedModel, test],
            [("Test.ReferencedModel.g.cs", GenerateReferencedModelCodeWithIsActive()),
             ("Test.TestModel.g.cs", expected)],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_MultiplePropertiesAccessed_GeneratesFilterWithAllProperties()
    {
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial int Count { get; set; }
            public partial string Status { get; set; } = "";
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                private void OnDataChanged()
                {
                    System.Console.WriteLine($"Count: {Referenced.Count}, Status: {Referenced.Status}");
                }
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
        using System.Linq;

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

                var usedProps = new[] { "Model.Referenced.Count", "Model.Referenced.Status" };

                return propertyNames.Intersect(usedProps).Any();
            }

            public Test.ReferencedModel Referenced { get; }



            public partial TestModel(Test.ReferencedModel referenced) : base()
            {
                Referenced = referenced;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Referenced.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Referenced.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));

                // Subscribe to internal model observers (auto-detected)
                Subscriptions.Add(Referenced.Observable.Where(p => p.Intersect(["Model.Count", "Model.Status"]).Any())
                    .Subscribe(_ => OnDataChanged()));
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
                Referenced.ContextReady();

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
                await Referenced.ContextReadyAsync();
            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [referencedModel, test],
            [("Test.ReferencedModel.g.cs", GenerateReferencedModelCodeWithCountAndStatus()),
             ("Test.TestModel.g.cs", expected)],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_NonPrivateMethod_DoesNotGenerateSubscription()
    {
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial int Value { get; set; }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                // Public method should NOT be auto-detected as internal observer
                // Note: Name deliberately avoids observer pattern (On*Changed) to not trigger RXBG082
                public void ProcessValue()
                {
                    System.Console.WriteLine($"Value: {Referenced.Value}");
                }
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
        using System.Linq;

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

                var usedProps = new[] { "Model.Referenced.Value" };

                return propertyNames.Intersect(usedProps).Any();
            }

            public Test.ReferencedModel Referenced { get; }



            public partial TestModel(Test.ReferencedModel referenced) : base()
            {
                Referenced = referenced;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Referenced.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Referenced.")).ToArray())
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
                Referenced.ContextReady();

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
                await Referenced.ContextReadyAsync();
            }
        
        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [referencedModel, test],
            [("Test.ReferencedModel.g.cs", GenerateReferencedModelCodeWithValue()),
             ("Test.TestModel.g.cs", expected)],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_CommandExecuteMethod_DoesNotGenerateSubscription()
    {
        // This test verifies the bug fix: command execute methods should NOT be auto-detected
        // as internal observers even if they access referenced model properties.
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial string CurrentUser { get; set; } = "";
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                // Command property
                [ObservableCommand(nameof(AddItem))]
                public partial IObservableCommand AddCommand { get; }

                // Command execute method - accesses Referenced.CurrentUser but should NOT be an observer
                private void AddItem()
                {
                    if (Referenced.CurrentUser is null) return;
                    System.Console.WriteLine($"Adding item for {Referenced.CurrentUser}");
                }

                // Actual observer method - should be auto-detected
                private void RefreshItems()
                {
                    System.Console.WriteLine($"Refreshing for {Referenced.CurrentUser}");
                }
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
        using System.Linq;

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

                var usedProps = new[] { "Model.Referenced.CurrentUser" };

                return propertyNames.Intersect(usedProps).Any();
            }

            public Test.ReferencedModel Referenced { get; }


            private ObservableCommand _addCommand;

            public partial IObservableCommand AddCommand
            {
                get => _addCommand;
            }

            public partial TestModel(Test.ReferencedModel referenced) : base()
            {
                Referenced = referenced;

                // Initialize commands
                _addCommand = new ObservableCommandFactory(this, [""], AddItem);

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Referenced.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Referenced.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));

                // Subscribe to internal model observers (auto-detected)
                Subscriptions.Add(Referenced.Observable.Where(p => p.Intersect(["Model.CurrentUser"]).Any())
                    .Subscribe(_ => RefreshItems()));
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
                Referenced.ContextReady();

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
                await Referenced.ContextReadyAsync();
            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [referencedModel, test],
            [("Test.ReferencedModel.g.cs", GenerateReferencedModelCodeWithCurrentUser()),
             ("Test.TestModel.g.cs", expected)],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_TriggerMethod_DoesNotGenerateSubscription()
    {
        // This test verifies: trigger methods should NOT be auto-detected as internal observers
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial string Format { get; set; } = "";
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                // Property with trigger
                [ObservableTrigger(nameof(OnValueChanged))]
                public partial int Value { get; set; }

                // Trigger method - accesses Referenced.Format but should NOT be an observer
                private void OnValueChanged()
                {
                    System.Console.WriteLine($"Value changed, format: {Referenced.Format}");
                }

                // Actual observer method - should be auto-detected
                private void RefreshDisplay()
                {
                    System.Console.WriteLine($"Format changed: {Referenced.Format}");
                }
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
        using System.Linq;

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

                var usedProps = new[] { "Model.Referenced.Format" };

                return propertyNames.Intersect(usedProps).Any();
            }

            public Test.ReferencedModel Referenced { get; }

            public partial int Value
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Value");
                    }
                }
            }


            public partial TestModel(Test.ReferencedModel referenced) : base()
            {
                Referenced = referenced;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Referenced.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Referenced.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));

                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Value"]).Any())
                    .Subscribe(_ => OnValueChanged()));

                // Subscribe to internal model observers (auto-detected)
                Subscriptions.Add(Referenced.Observable.Where(p => p.Intersect(["Model.Format"]).Any())
                    .Subscribe(_ => RefreshDisplay()));
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
                Referenced.ContextReady();

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
                await Referenced.ContextReadyAsync();
            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [referencedModel, test],
            [("Test.ReferencedModel.g.cs", GenerateReferencedModelCodeWithFormat()),
             ("Test.TestModel.g.cs", expected)],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_InSeparatePartialFile_GeneratesSubscription()
    {
        // This test verifies the bug fix: internal observers in separate partial files
        // should still be detected and generate subscriptions.
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class AuthModel : ObservableModel
        {
            public partial string CurrentUser { get; set; } = "";
        }
        """;

        // Main file with constructor and properties
        // lang=csharp
        const string mainFile = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(AuthModel auth);

                public partial string Data { get; set; } = "";
            }
        }
        """;

        // Separate partial file with internal observer method
        // lang=csharp
        const string observersFile = """
        namespace Test
        {
            public partial class TestModel
            {
                // Internal observer - accesses Auth.CurrentUser
                // Should be auto-detected even though it's in a separate partial file
                private void RefreshData()
                {
                    if (Auth.CurrentUser is not null)
                    {
                        Data = $"Data for {Auth.CurrentUser}";
                    }
                }
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
        using System.Linq;

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

            public Test.AuthModel Auth { get; }

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


            public partial TestModel(Test.AuthModel auth) : base()
            {
                Auth = auth;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Auth.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Auth.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));

                // Subscribe to internal model observers (auto-detected)
                Subscriptions.Add(Auth.Observable.Where(p => p.Intersect(["Model.CurrentUser"]).Any())
                    .Subscribe(_ => RefreshData()));
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
                Auth.ContextReady();

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
                await Auth.ContextReadyAsync();
            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [referencedModel, mainFile, observersFile],
            [("Test.AuthModel.g.cs", GenerateAuthModelCode()),
             ("Test.TestModel.g.cs", expected)],
            ["AuthModel", "TestModel"]);
    }

    #endregion

    #region External Model Observer Tests

    [Fact]
    public async Task ExternalModelObserver_SyncMethod_GeneratesOnContextReadyIntern()
    {
        // lang=csharp
        const string service = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public class TestService
            {
                [ObservableModelObserver(nameof(TestModel.Name))]
                public void OnNameChanged(TestModel model)
                {
                    System.Console.WriteLine($"Name changed: {model.Name}");
                }
            }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; } = "";

                public partial TestModel(TestService service);
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

            protected Test.TestService Service { get; }

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


            public partial TestModel(Test.TestService service) : base()
            {
                Service = service;
            }
            private bool _contextReadyInternCalled;
        
            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;

                // Subscribe to model observers in services
                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Name"]).Any())
                    .Subscribe(_ => Service.OnNameChanged(this)));

            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [service, test],
            [("Test.TestModel.g.cs", expected)],
            ["TestModel"]);
    }

    [Fact]
    public async Task ExternalModelObserver_AsyncMethodWithCancellationToken_GeneratesSubscribeAwait()
    {
        // lang=csharp
        const string service = """
        using RxBlazorV2.Model;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Test
        {
            public class TestService
            {
                [ObservableModelObserver(nameof(TestModel.Count))]
                public async Task OnCountChangedAsync(TestModel model, CancellationToken ct)
                {
                    await Task.Delay(100, ct);
                    System.Console.WriteLine($"Count: {model.Count}");
                }
            }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial int Count { get; set; }

                public partial TestModel(TestService service);
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

            protected Test.TestService Service { get; }

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


            public partial TestModel(Test.TestService service) : base()
            {
                Service = service;
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;
        
                // Subscribe to model observers in services
                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Count"]).Any())
                    .SubscribeAwait(async (_, ct) =>
                    {
                        await Service.OnCountChangedAsync(this, ct);
                    }, AwaitOperation.Switch));

            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [service, test],
            [("Test.TestModel.g.cs", expected)],
            ["TestModel"]);
    }

    [Fact]
    public async Task ExternalModelObserver_AsyncMethodWithoutCancellationToken_GeneratesSubscribeAwait()
    {
        // lang=csharp
        const string service = """
        using RxBlazorV2.Model;
        using System.Threading.Tasks;

        namespace Test
        {
            public class TestService
            {
                [ObservableModelObserver(nameof(TestModel.Status))]
                public async Task OnStatusChangedAsync(TestModel model)
                {
                    await Task.Delay(50);
                    System.Console.WriteLine($"Status: {model.Status}");
                }
            }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial string Status { get; set; } = "";

                public partial TestModel(TestService service);
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

            protected Test.TestService Service { get; }

            public partial string Status
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Status");
                    }
                }
            }


            public partial TestModel(Test.TestService service) : base()
            {
                Service = service;
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;
        
                // Subscribe to model observers in services
                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Status"]).Any())
                    .SubscribeAwait(async (_, _) =>
                    {
                        await Service.OnStatusChangedAsync(this);
                    }, AwaitOperation.Switch));

            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [service, test],
            [("Test.TestModel.g.cs", expected)],
            ["TestModel"]);
    }

    [Fact]
    public async Task ExternalModelObserver_MultipleObserversOnSameProperty_GeneratesMultipleSubscriptions()
    {
        // lang=csharp
        const string service = """
        using RxBlazorV2.Model;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Test
        {
            public class TestService
            {
                [ObservableModelObserver(nameof(TestModel.Value))]
                public void OnValueChangedSync(TestModel model)
                {
                    System.Console.WriteLine($"Sync: {model.Value}");
                }

                [ObservableModelObserver(nameof(TestModel.Value))]
                public async Task OnValueChangedAsync(TestModel model, CancellationToken ct)
                {
                    await Task.Delay(100, ct);
                    System.Console.WriteLine($"Async: {model.Value}");
                }
            }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial int Value { get; set; }

                public partial TestModel(TestService service);
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

            protected Test.TestService Service { get; }

            public partial int Value
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Value");
                    }
                }
            }


            public partial TestModel(Test.TestService service) : base()
            {
                Service = service;
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;

                // Subscribe to model observers in services
                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Value"]).Any())
                    .Subscribe(_ => Service.OnValueChangedSync(this)));

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Value"]).Any())
                    .SubscribeAwait(async (_, ct) =>
                    {
                        await Service.OnValueChangedAsync(this, ct);
                    }, AwaitOperation.Switch));

            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [service, test],
            [("Test.TestModel.g.cs", expected)],
            ["TestModel"]);
    }

    [Fact]
    public async Task ExternalModelObserver_MultipleAttributesOnMethod_GeneratesSubscriptionForEachProperty()
    {
        // lang=csharp
        const string service = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public class TestService
            {
                [ObservableModelObserver(nameof(TestModel.Name))]
                [ObservableModelObserver(nameof(TestModel.Age))]
                public void OnDataChanged(TestModel model)
                {
                    System.Console.WriteLine($"Name: {model.Name}, Age: {model.Age}");
                }
            }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial string Name { get; set; } = "";
                public partial int Age { get; set; }

                public partial TestModel(TestService service);
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

            protected Test.TestService Service { get; }

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

            public partial int Age
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Age");
                    }
                }
            }


            public partial TestModel(Test.TestService service) : base()
            {
                Service = service;
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;

                // Subscribe to model observers in services
                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Name"]).Any())
                    .Subscribe(_ => Service.OnDataChanged(this)));

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Age"]).Any())
                    .Subscribe(_ => Service.OnDataChanged(this)));

            }

        }

        """;

        await MultiModelGeneratorVerifier.VerifyMultiModelGeneratorAsync(
            [service, test],
            [("Test.TestModel.g.cs", expected)],
            ["TestModel"]);
    }

    #endregion

    #region RXBG082 Diagnostic Tests

    [Fact]
    public async Task InternalModelObserver_PublicMethodWithObserverName_GeneratesRXBG082()
    {
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial int Counter { get; set; }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                // Public method with observer naming pattern - should generate RXBG082
                public void OnCounterChanged()
                {
                    System.Console.WriteLine($"Counter: {Referenced.Counter}");
                }
            }
        }
        """;

        var expected = Microsoft.CodeAnalysis.Testing.DiagnosticResult
            .CompilerWarning("RXBG082")
            .WithSpan("Source1.cs", 10, 21, 10, 37)
            .WithArguments("OnCounterChanged", "Counter", "Referenced", "Method must be private to be auto-detected as internal observer.");

        await MultiModelGeneratorVerifierWithDiagnostics.VerifyAsync(
            [referencedModel, test],
            [expected],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_PrivateMethodWithParameter_DoesNotGenerateRXBG082WhenNamedDifferently()
    {
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial int Value { get; set; }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                // Private method with parameter but not observer-like name - should NOT generate RXBG082
                private void DoSomething(int param)
                {
                    System.Console.WriteLine($"Value: {Referenced.Value}");
                }
            }
        }
        """;

        // No diagnostics expected - method name doesn't look like an observer
        await MultiModelGeneratorVerifierWithDiagnostics.VerifyAsync(
            [referencedModel, test],
            [],
            ["ReferencedModel", "TestModel"]);
    }

    [Fact]
    public async Task InternalModelObserver_PrivateMethodWithWrongReturnType_GeneratesRXBG082()
    {
        // lang=csharp
        const string referencedModel = """
        using RxBlazorV2.Model;

        namespace Test;

        public partial class ReferencedModel : ObservableModel
        {
            public partial bool IsEnabled { get; set; }
        }
        """;

        // lang=csharp
        const string test = """
        using RxBlazorV2.Model;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial TestModel(ReferencedModel referenced);

                // Private method with observer name but wrong return type
                private bool OnIsEnabledChanged()
                {
                    return Referenced.IsEnabled;
                }
            }
        }
        """;

        var expected = Microsoft.CodeAnalysis.Testing.DiagnosticResult
            .CompilerWarning("RXBG082")
            .WithSpan("Source1.cs", 10, 22, 10, 40)
            .WithArguments("OnIsEnabledChanged", "IsEnabled", "Referenced", "Return type must be void, Task, or ValueTask. Found 'bool'.");

        await MultiModelGeneratorVerifierWithDiagnostics.VerifyAsync(
            [referencedModel, test],
            [expected],
            ["ReferencedModel", "TestModel"]);
    }

    #endregion

    #region Helper Methods for Expected Generated Code

    private static string GenerateReferencedModelCode()
    {
        return """
        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ReferencedModel
        {
            public override string ModelID => "Test.ReferencedModel";

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
    }

    private static string GenerateReferencedModelCodeWithName()
    {
        return """
        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ReferencedModel
        {
            public override string ModelID => "Test.ReferencedModel";

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
    }

    private static string GenerateReferencedModelCodeWithIsActive()
    {
        return """
        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ReferencedModel
        {
            public override string ModelID => "Test.ReferencedModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
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
    }

    private static string GenerateReferencedModelCodeWithCountAndStatus()
    {
        return """
        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ReferencedModel
        {
            public override string ModelID => "Test.ReferencedModel";

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

            public partial string Status
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Status");
                    }
                }
            }

        }

        """;
    }

    private static string GenerateReferencedModelCodeWithValue()
    {
        return """
        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ReferencedModel
        {
            public override string ModelID => "Test.ReferencedModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial int Value
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Value");
                    }
                }
            }

        }

        """;
    }

    private static string GenerateReferencedModelCodeWithCurrentUser()
    {
        return """
        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ReferencedModel
        {
            public override string ModelID => "Test.ReferencedModel";

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

        }

        """;
    }

    private static string GenerateReferencedModelCodeWithFormat()
    {
        return """
        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ReferencedModel
        {
            public override string ModelID => "Test.ReferencedModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial string Format
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Format");
                    }
                }
            }

        }

        """;
    }

    private static string GenerateAuthModelCode()
    {
        return """
        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class AuthModel
        {
            public override string ModelID => "Test.AuthModel";

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

        }

        """;
    }

    #endregion
}

/// <summary>
/// Verifier for tests that involve multiple model files
/// </summary>
internal static class MultiModelGeneratorVerifier
{
    public static Task VerifyMultiModelGeneratorAsync(
        string[] sources,
        (string FileName, string Content)[] expectedGenerated,
        string[] modelNames)
    {
        var test = new MultiModelGeneratorTest(sources, expectedGenerated, modelNames);
        return test.RunAsync();
    }
}

internal class MultiModelGeneratorTest : Microsoft.CodeAnalysis.CSharp.Testing.CSharpSourceGeneratorTest<RxBlazorV2Generator.RxBlazorGenerator, Microsoft.CodeAnalysis.Testing.DefaultVerifier>
{
    private readonly string[] _sources;
    private readonly (string FileName, string Content)[] _expectedGenerated;
    private readonly string[] _modelNames;

    public MultiModelGeneratorTest(string[] sources, (string FileName, string Content)[] expectedGenerated, string[] modelNames)
    {
        _sources = sources;
        _expectedGenerated = expectedGenerated;
        _modelNames = modelNames;
    }

    protected override Microsoft.CodeAnalysis.ParseOptions CreateParseOptions()
    {
        return new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(
            languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);
    }

    public async Task RunAsync()
    {
        // Add global usings
        TestState.Sources.Add(("GlobalUsings.cs", Microsoft.CodeAnalysis.Text.SourceText.From(TestShared.GlobalUsing, System.Text.Encoding.UTF8)));

        // Add source files
        for (int i = 0; i < _sources.Length; i++)
        {
            TestState.Sources.Add(($"Source{i}.cs", Microsoft.CodeAnalysis.Text.SourceText.From(_sources[i], System.Text.Encoding.UTF8)));
        }

        // Add expected generated files
        foreach (var (fileName, content) in _expectedGenerated)
        {
            TestState.GeneratedSources.Add((typeof(RxBlazorV2Generator.RxBlazorGenerator), fileName,
                Microsoft.CodeAnalysis.Text.SourceText.From(content.TrimStart().Replace("\r\n", Environment.NewLine), System.Text.Encoding.UTF8)));
        }

        // Add service extension files
        TestState.GeneratedSources.Add((typeof(RxBlazorV2Generator.RxBlazorGenerator), "ObservableModelsServiceCollectionExtension.g.cs",
            Microsoft.CodeAnalysis.Text.SourceText.From(GenerateServiceExtension(), System.Text.Encoding.UTF8)));
        TestState.GeneratedSources.Add((typeof(RxBlazorV2Generator.RxBlazorGenerator), "GenericModelsServiceCollectionExtension.g.cs",
            Microsoft.CodeAnalysis.Text.SourceText.From(GenerateGenericServiceExtension(), System.Text.Encoding.UTF8)));

        TestState.ReferenceAssemblies = TestShared.ReferenceAssemblies();
        TestState.AdditionalReferences.Add(typeof(RxBlazorV2.Model.ObservableModel).Assembly);

        // Disable RXBG050 diagnostic for these tests (unregistered service warning)
        // We're not testing DI registration here
        DisabledDiagnostics.Add("RXBG050");

        await base.RunAsync(CancellationToken.None);
    }

    private string GenerateServiceExtension()
    {
        var registrations = string.Join(Environment.NewLine, _modelNames.Select(m => $"        services.AddScoped<{m}>();"));

        var result = $$"""
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.Model;
using Test;

namespace Global;

public static partial class ObservableModels
{
    public static IServiceCollection Initialize(IServiceCollection services)
    {
{{registrations}}
        return services;
    }
}
""";
        return result.Replace("\r\n", Environment.NewLine) + Environment.NewLine;
    }

    private string GenerateGenericServiceExtension()
    {
        var result = """
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.Model;
using Test;

namespace Global;

public static partial class ObservableModels
{
}
""";
        return result.TrimStart().Replace("\r\n", Environment.NewLine) + Environment.NewLine;
    }
}

/// <summary>
/// Verifier for tests that check RXBG082 diagnostics
/// </summary>
internal static class MultiModelGeneratorVerifierWithDiagnostics
{
    public static Task VerifyAsync(
        string[] sources,
        Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expectedDiagnostics,
        string[] modelNames)
    {
        var test = new MultiModelGeneratorDiagnosticTest(sources, expectedDiagnostics, modelNames);
        return test.RunAsync();
    }
}

internal class MultiModelGeneratorDiagnosticTest : Microsoft.CodeAnalysis.CSharp.Testing.CSharpSourceGeneratorTest<RxBlazorV2Generator.RxBlazorGenerator, Microsoft.CodeAnalysis.Testing.DefaultVerifier>
{
    private readonly string[] _sources;
    private readonly Microsoft.CodeAnalysis.Testing.DiagnosticResult[] _expectedDiagnostics;
    private readonly string[] _modelNames;

    public MultiModelGeneratorDiagnosticTest(string[] sources, Microsoft.CodeAnalysis.Testing.DiagnosticResult[] expectedDiagnostics, string[] modelNames)
    {
        _sources = sources;
        _expectedDiagnostics = expectedDiagnostics;
        _modelNames = modelNames;
    }

    protected override Microsoft.CodeAnalysis.ParseOptions CreateParseOptions()
    {
        return new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(
            languageVersion: Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);
    }

    public async Task RunAsync()
    {
        // Add global usings
        TestState.Sources.Add(("GlobalUsings.cs", Microsoft.CodeAnalysis.Text.SourceText.From(TestShared.GlobalUsing, System.Text.Encoding.UTF8)));

        // Add source files
        for (int i = 0; i < _sources.Length; i++)
        {
            TestState.Sources.Add(($"Source{i}.cs", Microsoft.CodeAnalysis.Text.SourceText.From(_sources[i], System.Text.Encoding.UTF8)));
        }

        // Add expected diagnostics
        foreach (var diagnostic in _expectedDiagnostics)
        {
            TestState.ExpectedDiagnostics.Add(diagnostic);
        }

        // Disable RXBG050 diagnostic for these tests (unregistered service warning)
        DisabledDiagnostics.Add("RXBG050");

        // Skip generated sources verification - we only care about diagnostics
        TestBehaviors = Microsoft.CodeAnalysis.Testing.TestBehaviors.SkipGeneratedSourcesCheck;

        TestState.ReferenceAssemblies = TestShared.ReferenceAssemblies();
        TestState.AdditionalReferences.Add(typeof(RxBlazorV2.Model.ObservableModel).Assembly);

        await base.RunAsync(CancellationToken.None);
    }
}
