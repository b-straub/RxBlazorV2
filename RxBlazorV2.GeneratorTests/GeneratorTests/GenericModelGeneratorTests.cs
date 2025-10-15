using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

public class GenericModelGeneratorTests
{
    [Fact]
    public async Task GenericModel_SingleTypeParameter_GeneratesCorrectCode()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModel<T> : ObservableModel where T : class
            {
                public partial T Item { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generated = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class GenericModel<T>
        {
            public override string ModelID => "Test.GenericModel<T>";

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

            public partial T Item
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    field = value;
                    StateHasChanged(nameof(Item));
                }
            }

        }

        """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, generated, "GenericModel<T>", "where T : class");
    }

    [Fact]
    public async Task GenericModel_MultipleTypeParameters_GeneratesCorrectCode()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModel<T, P> : ObservableModel where T : class where P : struct
            {
                public partial T Item1 { get; set; }
                public partial P Item2 { get; set; }
            }
        }
        """;

        // lang=csharp
        const string generated = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class GenericModel<T, P>
        {
            public override string ModelID => "Test.GenericModel<T, P>";

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;

            public partial T Item1
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    field = value;
                    StateHasChanged(nameof(Item1));
                }
            }

            public partial P Item2
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    field = value;
                    StateHasChanged(nameof(Item2));
                }
            }

        }

        """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, generated, "GenericModel<T, P>", "where T : class where P : struct");
    }

    [Fact]
    public async Task GenericModel_WithCommand_GeneratesCorrectFactory()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            [ObservableModelScope(ModelScope.Singleton)]
            public partial class GenericModel<T> : ObservableModel where T : class
            {
                [ObservableCommand(nameof(ExecuteMethod))]
                public partial IObservableCommand<T> TestCommand { get; }

                private void ExecuteMethod(T item)
                {
                }
            }
        }
        """;

        // lang=csharp
        const string generated = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class GenericModel<T>
        {
            public override string ModelID => "Test.GenericModel<T>";

            private readonly CompositeDisposable _subscriptions = new();
            protected override IDisposable Subscriptions => _subscriptions;


            private ObservableCommand<T> _testCommand;

            public partial IObservableCommand<T> TestCommand
            {
                get => _testCommand;
            }

            public GenericModel() : base()
            {
                // Initialize commands
                _testCommand = new ObservableCommandFactory<T>(this, [""], ExecuteMethod);
            }
        }

        """;

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, generated, "GenericModel<T>", "where T : class");
    }

    /*
    [Fact]
    public async Task GenericModel_OpenGenericReference_GeneratesCorrectProperty()
    {
        // lang=csharp
        const string test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel where T : class
    {
        public partial T Item { get; set; }
    }

    [ObservableModelReference(typeof(GenericModel<,>))]
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class ConsumerModel<T, P> : ObservableModel where T : class where P : struct
    {
        public partial int Value { get; set; }
    }
}";

        // lang=csharp
        const string generated = @"
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using ObservableCollections;
using R3;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using System;
using System.Linq;

namespace Test;

public partial class ConsumerModel<T, P> where T : class where P : struct
{
    public override string ModelID => ""Test.ConsumerModel<T, P>"";

    private readonly CompositeDisposable _subscriptions = new();
    protected override IDisposable Subscriptions => _subscriptions;

    protected GenericModel<T> GenericModel { get; private set; }

    public partial int Value
    {
        get => field;
        set
        {
            field = value;
            StateHasChanged(nameof(Value));
        }
    }

    public ConsumerModel(GenericModel<T> genericmodel) : base()
    {
        GenericModel = genericmodel;

        // Subscribe to referenced model changes
        _subscriptions.Add(GenericModel.Observable.Where(p => p.Intersect([""""]).Any())
            .Chunk(TimeSpan.FromMilliseconds(100))
            .Subscribe(chunks => StateHasChanged(chunks.SelectMany(c => c).ToArray())));
    }
}
";

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, generated, "ConsumerModel", true);
    }

    [Fact]
    public async Task GenericModel_SeparateDI_RegistrationMethod()
    {
        // lang=csharp
        const string test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    [ObservableModelScope(ModelScope.Singleton)]
    public partial class GenericModel<T> : ObservableModel where T : class
    {
        public partial T Item { get; set; }
    }
}";

        // lang=csharp
        const string generated = @"
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.Model;
using Test;

namespace Test;

public static partial class ObservableModels
{
    public static IServiceCollection GenericModel<T>(IServiceCollection services)
        where T : class
    {
        services.AddSingleton<GenericModel<T>>();
        return services;
    }

}
";

        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, generated, "GenericModelsServiceCollectionExtension", true);
    }
    */
}
