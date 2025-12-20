using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for cross-assembly inheritance - ensuring derived classes in a different assembly
/// correctly inherit triggers from base classes in the RxBlazorV2 assembly.
/// </summary>
public class CrossAssemblyInheritanceTests
{
    /// <summary>
    /// Tests that a class inheriting from an abstract base with trigger attributes
    /// generates the correct hooks for inherited [ObservableComponentTrigger] and [ObservableTrigger].
    /// </summary>
    [Fact]
    public async Task AbstractBaseWithTriggers_GeneratesInheritedTriggerHooks()
    {
        // Define own abstract base class with trigger attributes for testing
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            /// <summary>
            /// Test abstract base class with trigger attributes on abstract members.
            /// </summary>
            public abstract class TestStatusBaseModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public abstract ObservableList<string> Messages { get; }

                [ObservableComponentTrigger]
                [ObservableTrigger(nameof(CanAddMessageTrigger))]
                public abstract bool CanAddMessage { get; set; }

                [ObservableCommandTrigger(nameof(CanAddMessage))]
                public abstract IObservableCommand AddMessageCommand { get; }

                protected abstract void CanAddMessageTrigger();
            }

            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class MyStatusModel : TestStatusBaseModel
            {
                // Required implementations for abstract members
                public override ObservableList<string> Messages { get; } = [];
                public override partial bool CanAddMessage { get; set; }

                [ObservableCommand(nameof(AddMessage))]
                public override partial IObservableCommand AddMessageCommand { get; }

                protected override void CanAddMessageTrigger() { }
                private void AddMessage() { }
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

        public partial class MyStatusModel
        {
            public override string ModelID => "Test.MyStatusModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public override partial bool CanAddMessage
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.CanAddMessage");
                    }
                }
            }

            private ObservableCommand _addMessageCommand;

            public override partial IObservableCommand AddMessageCommand
            {
                get => _addMessageCommand;
            }

            public MyStatusModel()
            {

                // Initialize IObservableCollection properties
                Subscriptions.Add(Messages.ObserveChanged()
                    .Subscribe(_ => StateHasChanged("Model.Messages")));

                // Initialize commands
                _addMessageCommand = new ObservableCommandFactory(this, [""], "AddMessageCommand", "AddMessage", AddMessage);

                // Subscribe to command triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Subscribe(_ => _addMessageCommand.Execute()));

                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Subscribe(_ => CanAddMessageTrigger()));
            }
        }

        """;

        // The component must generate OnMessagesChanged() and OnCanAddMessageChanged() hooks for inherited triggers from StatusBaseModel
        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class MyStatusModelComponent : ObservableComponent<MyStatusModel>
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Messages"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnMessagesChanged();
                    }));

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnCanAddMessageChanged();
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnMessagesChanged()
            {
            }

            protected virtual void OnCanAddMessageChanged()
            {
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, generatedModel, generatedComponent,
            "MyStatusModel", "MyStatusModelComponent");
    }
}


/// <summary>
/// Tests for inheritance flattening - ensuring derived ObservableModel classes
/// generate reactive code for inherited properties, triggers, and commands.
///
/// This is critical for scenarios where:
/// - Base class is abstract (e.g., StatusBaseModel)
/// - Base class is in a different assembly (cross-assembly inheritance)
/// - Derived class is "empty" but relies on inherited reactive elements
/// </summary>
public class InheritanceFlatteningTests
{
    /// <summary>
    /// Tests that a class inheriting from an abstract base with trigger attributes
    /// generates the OnMessagesChanged() and OnCanAddMessageChanged() hooks for inherited triggers.
    /// </summary>
    [Fact]
    public async Task DerivedFromAbstractBase_GeneratesInheritedComponentTriggerHook()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            /// <summary>
            /// Test abstract base class with trigger attributes.
            /// </summary>
            public abstract class TestStatusBaseModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public abstract ObservableList<string> Messages { get; }

                [ObservableComponentTrigger]
                [ObservableTrigger(nameof(CanAddMessageTrigger))]
                public abstract bool CanAddMessage { get; set; }

                [ObservableCommandTrigger(nameof(CanAddMessage))]
                public abstract IObservableCommand AddMessageCommand { get; }

                protected abstract void CanAddMessageTrigger();
            }

            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class AppStatusModel : TestStatusBaseModel
            {
                // Required implementations for abstract members
                public override ObservableList<string> Messages { get; } = [];
                public override partial bool CanAddMessage { get; set; }

                [ObservableCommand(nameof(AddMessage))]
                public override partial IObservableCommand AddMessageCommand { get; }

                protected override void CanAddMessageTrigger() { }
                private void AddMessage() { }
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

        public partial class AppStatusModel
        {
            public override string ModelID => "Test.AppStatusModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public override partial bool CanAddMessage
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.CanAddMessage");
                    }
                }
            }

            private ObservableCommand _addMessageCommand;

            public override partial IObservableCommand AddMessageCommand
            {
                get => _addMessageCommand;
            }

            public AppStatusModel()
            {

                // Initialize IObservableCollection properties
                Subscriptions.Add(Messages.ObserveChanged()
                    .Subscribe(_ => StateHasChanged("Model.Messages")));

                // Initialize commands
                _addMessageCommand = new ObservableCommandFactory(this, [""], "AddMessageCommand", "AddMessage", AddMessage);

                // Subscribe to command triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Subscribe(_ => _addMessageCommand.Execute()));

                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Subscribe(_ => CanAddMessageTrigger()));
            }
        }

        """;

        // The component must generate OnMessagesChanged() and OnCanAddMessageChanged() hooks for inherited triggers
        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class AppStatusModelComponent : ObservableComponent<AppStatusModel>
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Messages"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnMessagesChanged();
                    }));

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnCanAddMessageChanged();
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnMessagesChanged()
            {
            }

            protected virtual void OnCanAddMessageChanged()
            {
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, generatedModel, generatedComponent,
            "AppStatusModel", "AppStatusModelComponent");
    }

    /// <summary>
    /// Tests that a derived class with its own properties combined with inherited triggers
    /// generates code for both own and inherited reactive elements.
    /// </summary>
    [Fact]
    public async Task DerivedWithOwnProperties_GeneratesBothOwnAndInheritedReactiveCode()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            /// <summary>
            /// Test abstract base class with trigger attributes.
            /// </summary>
            public abstract class TestStatusBaseModel : ObservableModel
            {
                [ObservableComponentTrigger]
                public abstract ObservableList<string> Messages { get; }

                [ObservableComponentTrigger]
                [ObservableTrigger(nameof(CanAddMessageTrigger))]
                public abstract bool CanAddMessage { get; set; }

                [ObservableCommandTrigger(nameof(CanAddMessage))]
                public abstract IObservableCommand AddMessageCommand { get; }

                protected abstract void CanAddMessageTrigger();
            }

            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ExtendedStatusModel : TestStatusBaseModel
            {
                // Required implementations for abstract members
                public override ObservableList<string> Messages { get; } = [];
                public override partial bool CanAddMessage { get; set; }

                [ObservableCommand(nameof(AddMessage))]
                public override partial IObservableCommand AddMessageCommand { get; }

                protected override void CanAddMessageTrigger() { }
                private void AddMessage() { }

                // Own property in addition to inherited Messages
                public partial bool IsPanelExpanded { get; set; }
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

        public partial class ExtendedStatusModel
        {
            public override string ModelID => "Test.ExtendedStatusModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public override partial bool CanAddMessage
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.CanAddMessage");
                    }
                }
            }

            public partial bool IsPanelExpanded
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.IsPanelExpanded");
                    }
                }
            }

            private ObservableCommand _addMessageCommand;

            public override partial IObservableCommand AddMessageCommand
            {
                get => _addMessageCommand;
            }

            public ExtendedStatusModel()
            {

                // Initialize IObservableCollection properties
                Subscriptions.Add(Messages.ObserveChanged()
                    .Subscribe(_ => StateHasChanged("Model.Messages")));

                // Initialize commands
                _addMessageCommand = new ObservableCommandFactory(this, [""], "AddMessageCommand", "AddMessage", AddMessage);

                // Subscribe to command triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Subscribe(_ => _addMessageCommand.Execute()));

                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Subscribe(_ => CanAddMessageTrigger()));
            }
        }

        """;

        // The component must generate OnMessagesChanged() and OnCanAddMessageChanged() hooks for inherited triggers
        // AND handle own property changes
        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class ExtendedStatusModelComponent : ObservableComponent<ExtendedStatusModel>
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Messages"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnMessagesChanged();
                    }));

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnCanAddMessageChanged();
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnMessagesChanged()
            {
            }

            protected virtual void OnCanAddMessageChanged()
            {
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, generatedModel, generatedComponent,
            "ExtendedStatusModel", "ExtendedStatusModelComponent");
    }

    /// <summary>
    /// Tests that component triggers defined in abstract base class with multiple levels of inheritance
    /// are correctly propagated to the final derived class.
    /// </summary>
    [Fact]
    public async Task AbstractBaseWithTrigger_DerivedGeneratesHook()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using ObservableCollections;

        namespace Test
        {
            public abstract class BaseWithTrigger : ObservableModel
            {
                [ObservableComponentTrigger]
                public ObservableList<string> Items { get; } = [];
            }

            [ObservableComponent]
            [ObservableModelScope(ModelScope.Scoped)]
            public partial class ConcreteModel : BaseWithTrigger
            {
                // Empty - only inherits from BaseWithTrigger
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

        public partial class ConcreteModel
        {
            public override string ModelID => "Test.ConcreteModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }


        }

        """;

        // The component must generate OnItemsChanged() hook for inherited trigger
        // lang=csharp
        const string generatedComponent = """

        using R3;
        using ObservableCollections;
        using System;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Component;

        namespace Test;

        public partial class ConcreteModelComponent : ObservableComponent<ConcreteModel>
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

                Subscriptions.Add(Model.Observable.Where(p => p.Intersect(["Model.Items"]).Any())
                    .Chunk(TimeSpan.FromMilliseconds(100))
                    .Subscribe(chunks =>
                    {
                        OnItemsChanged();
                    }));
            }

            protected override Task InitializeGeneratedCodeAsync()
            {
                return Task.CompletedTask;
            }

            protected virtual void OnItemsChanged()
            {
            }
        }

        """;

        await ComponentGeneratorVerifier.VerifyComponentGeneratorAsync(
            test, generatedModel, generatedComponent,
            "ConcreteModel", "ConcreteModelComponent");
    }
}
