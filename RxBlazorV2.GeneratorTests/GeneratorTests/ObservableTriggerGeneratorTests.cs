using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

/// <summary>
/// Tests for ObservableTrigger code generation to ensure correct filter format with qualified property names.
/// </summary>
public class ObservableTriggerGeneratorTests
{
    [Fact]
    public async Task BasicPropertyTrigger_GeneratesQualifiedPropertyName()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                [ObservableTrigger(nameof(OnMessageChanged))]
                public partial string Message { get; set; } = "";

                private void OnMessageChanged()
                {
                    System.Console.WriteLine($"Message changed: {Message}");
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

            public partial string Message
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Message");
                    }
                }
            }


            public TestModel() : base()
            {
                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Message"]).Any())
                    .Subscribe(_ => OnMessageChanged()));
            }
        }

        """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "TestModel", string.Empty);
    }

    [Fact]
    public async Task PropertyTriggerWithDifferentPropertyName_GeneratesQualifiedPropertyName()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class ErrorModel : ObservableModel
            {
                [ObservableTrigger(nameof(ShowError))]
                public partial string Message { get; set; } = "";

                private void ShowError()
                {
                    System.Console.WriteLine($"Error: {Message}");
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

        namespace Test;

        public partial class ErrorModel
        {
            public override string ModelID => "Test.ErrorModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial string Message
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Message");
                    }
                }
            }


            public ErrorModel() : base()
            {
                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Message"]).Any())
                    .Subscribe(_ => ShowError()));
            }
        }

        """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "ErrorModel", string.Empty);
    }

    [Fact]
    public async Task MultipleTriggersOnSameProperty_AllUseQualifiedNames()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                [ObservableTrigger(nameof(Validate))]
                [ObservableTrigger(nameof(LogChange))]
                public partial string Input { get; set; } = "";

                private void Validate()
                {
                    System.Console.WriteLine("Validating...");
                }

                private void LogChange()
                {
                    System.Console.WriteLine("Logging...");
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

            public partial string Input
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Input");
                    }
                }
            }


            public TestModel() : base()
            {
                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Input"]).Any())
                    .Subscribe(_ => Validate()));

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Input"]).Any())
                    .Subscribe(_ => LogChange()));
            }
        }

        """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "TestModel", string.Empty);
    }

    [Fact]
    public async Task AsyncTrigger_GeneratesQualifiedPropertyName()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;
        using System.Threading.Tasks;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                [ObservableTrigger(nameof(SaveAsync))]
                public partial string Data { get; set; } = "";

                private async Task SaveAsync()
                {
                    await Task.Delay(100);
                    System.Console.WriteLine($"Saved: {Data}");
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

                // No filtering information available - pass through all
                return true;
            }

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


            public TestModel() : base()
            {
                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Data"]).Any())
                    .SubscribeAwait(async (_, ct) => await SaveAsync(), AwaitOperation.Switch));
            }
        }

        """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "TestModel", string.Empty);
    }

    [Fact]
    public async Task TriggerWithCanTrigger_GeneratesQualifiedPropertyName()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                [ObservableTrigger(nameof(UpdateValue), nameof(CanUpdate))]
                public partial int Count { get; set; }

                private void UpdateValue()
                {
                    System.Console.WriteLine($"Count updated: {Count}");
                }

                private bool CanUpdate()
                {
                    return Count > 0;
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


            public TestModel() : base()
            {
                // Subscribe property triggers

                Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Count"]).Any())
                    .Where(_ => CanUpdate())
                    .Subscribe(_ => UpdateValue()));
            }
        }

        """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, expected, "TestModel", string.Empty);
    }
}
