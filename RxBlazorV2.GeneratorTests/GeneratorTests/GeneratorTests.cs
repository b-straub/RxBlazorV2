using RxBlazorV2.GeneratorTests.Helpers;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

public class GeneratorTests
{
    [Fact]
    public async Task GeneratorTest_Simple()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial int Test { get; set; }
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

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public partial int Test
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Test");
                    }
                }
            }

        }

        """;
        
        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, generated, "TestModel", string.Empty);
    }
}